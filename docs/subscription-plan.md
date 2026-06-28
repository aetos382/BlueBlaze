# Plan: Lexicon Subscription サポートの実装

## Context

BlueBlaze は AT Protocol の .NET クライアントライブラリで、Lexicon JSON スキーマから C# コードを自動生成する。現在は Query（HTTP GET）と Procedure（HTTP POST）が実装済みだが、Subscription（WebSocket ストリーム）は未対応。

AT Protocol の Subscription は `wss://host/xrpc/{nsid}` への WebSocket 接続で、バイナリフレームごとに「CBOR ヘッダー + CBOR ペイロード」の 2 要素を受け取るプロトコル。ヘッダーの `t` フィールド（例: `"#commit"`）でメッセージ型を判別し、ペイロードを型ごとにデシリアライズする。

## 設計方針

- クライアント API: `IAsyncEnumerable<TMessage>` を返す
- CBOR ライブラリ: `System.Formats.Cbor`（net10.0 は BCL 内蔵、netstandard2.0 は NuGet 参照追加）
- Union 型: 既存の `unionMemberImpls` パターンを流用して `IMessage` マーカーインターフェースを生成し、各メッセージクラス（`MessageCommit` 等）が実装する
- CBOR ヘッダーパース（op/t 読み取り）はコアライブラリ側、ペイロードデシリアライズは生成コード側に置く
- CBOR デシリアライズは生成コードに埋め込む（HTTP 側の JSON デシリアライザと対称）

## インターフェース・メソッド命名方針

`ILexiconRequest` を 3 つに分割し、`HttpMethod` プロパティは廃止（操作種別は型で表現）：

| 旧 | 新 | Nsid | Parameters | Input |
|----|----|----|----|----|
| `ILexiconRequest` (Query) | `IQueryRequest` | ✓ | ✓ | - |
| `ILexiconRequest` (Procedure) | `IProcedureRequest` | ✓ | - | ✓ |
| （新規）Subscription | `ISubscribeRequest` | ✓ | ✓ | - |

`IAtProtocolClient` のメソッド名：
- `SendAsync` → `QueryAsync<TOutput>` / `ProcedureAsync<TOutput>`
- 新規 → `SubscribeAsync<TMessage>`

## 変更ファイル一覧

### Sources/Core（コアランタイム）

| ファイル | 変更種別 | 内容 |
|---------|---------|------|
| `LexiconOperationKind.cs` | 修正 | `Subscription` 値を追加 |
| `LexiconError.cs` | 修正 | `Description` → `Message` にリネーム（仕様の `message` フィールドに対応） |
| `LexiconException.cs` | 修正 | `sealed` を外し、標準コンストラクタのみの基底クラスに縮小（HTTP 固有メンバーは `LexiconHttpException` へ移動） |
| `LexiconHttpException.cs` | 新規 | Query/Procedure の HTTP エラー用（旧 `LexiconException` の内容: `RequestUri`, `StatusCode`, `ResponseHeaders`, `LexiconError?`） |
| `LexiconSubscriptionException.cs` | 新規 | WebSocket エラー用（`LexiconException` 派生、`LexiconError? Error` を持つ） |
| `ILexiconRequest.cs` | 修正 | `IQueryRequest` に改名、`HttpMethod` 削除、`Input` 削除 |
| `IProcedureRequest.cs` | 新規 | `Nsid`, `Input` を持つインターフェース |
| `ISubscribeRequest.cs` | 新規 | `Nsid`, `Parameters` を持つインターフェース |
| `IResponseDeserializer.cs` | 修正 | `IHttpResponseDeserializer` に改名 |
| `VoidOutputDeserializer.cs` | 修正 | `IHttpResponseDeserializer` に追従 |
| `RawJsonDeserializer.cs` | 修正 | `IHttpResponseDeserializer` に追従 |
| `ISubscriptionMessageDeserializer.cs` | 新規 | `Deserialize(string? messageType, ReadOnlySpan<byte> payload)` インターフェース |
| `IAtProtocolClient.cs` | 修正 | `SendAsync` → `QueryAsync`/`ProcedureAsync`、`SubscribeAsync<TMessage>` 追加 |
| `AtProtocolClient.cs` | 修正 | `SendAsync` を `QueryAsync`/`ProcedureAsync` に分割（`LexiconHttpException` を throw）、WebSocket 接続と CBOR フレームパース実装追加 |
| `Core.csproj` | 修正 | netstandard2.0 用に `System.Formats.Cbor`, `Microsoft.Bcl.AsyncInterfaces`, `System.Memory` パッケージ参照追加 |

`Directory.Packages.props` にバージョン未定義のパッケージは `dotnet package add` コマンドで追加する。

### Generators/LexiconGenerator/Core/Generation（コード生成）

| ファイル | 変更種別 | 内容 |
|---------|---------|------|
| `DocumentEmitter.cs` | 修正 | `EmitSubscription` に union ケースの `IMessage` インターフェース生成を追加 |
| `RequestEmitter.cs` | 修正 | `ILexiconRequest` → `IQueryRequest` / `IProcedureRequest` に対応 |
| `SubscriptionRequestEmitter.cs` | 新規 | `ISubscribeRequest` 実装クラス生成 |
| `DeserializerEmitter.cs` | 修正 | 生成クラスが `IHttpResponseDeserializer` を実装するように更新 |
| `SubscriptionDeserializerEmitter.cs` | 新規 | `ISubscriptionMessageDeserializer<IMessage>` 実装クラス生成（CBOR パース） |
| `ClientMethodEmitter.cs` | 修正 | Query → `QueryAsync`、Procedure → `ProcedureAsync`、Subscription → `SubscribeAsync` の呼び出しに変更 |

### Generators/LexiconGenerator/Core

| ファイル | 変更種別 | 内容 |
|---------|---------|------|
| `LexiconCodeGenerator.cs` | 修正 | Phase 3 に `SubscriptionDefinition` 処理分岐を追加 |

---

## 実装詳細

### 1. コアインターフェース・例外クラス

```csharp
// LexiconException.cs — 基底クラスに縮小（標準コンストラクタのみ）
public class LexiconException : Exception
{
    public LexiconException() : base("Lexicon error") { }
    public LexiconException(string message) : base(message) { }
    public LexiconException(string message, Exception inner) : base(message, inner) { }
}

// LexiconHttpException.cs — 旧 LexiconException の内容を移動
public sealed class LexiconHttpException : LexiconException
{
    public Uri RequestUri { get; }
    public HttpStatusCode StatusCode { get; }
    public HttpResponseHeaders ResponseHeaders { get; }
    public LexiconError? Error { get; }
    // ...
}

// LexiconSubscriptionException.cs — WebSocket エラー（接続確立失敗 & op=-1 両方）
public sealed class LexiconSubscriptionException : LexiconException
{
    public LexiconError? Error { get; }  // op=-1 フレームの { error, message }
    // 接続確立失敗時は Error=null で message のみ設定
}

// ISubscribeRequest.cs
public interface ISubscribeRequest
{
    string Nsid { get; }
    ILexiconParameters? Parameters { get; }
}

// ISubscriptionMessageDeserializer.cs
public interface ISubscriptionMessageDeserializer<TMessage>
{
    TMessage? Deserialize(string? messageType, ReadOnlySpan<byte> payload);
}
```

**エラーの2パターン：**
- 接続確立失敗（HTTP ハンドシェイク 4xx/5xx）→ `LexiconSubscriptionException`（`Error` = null, `InnerException` = 原因の例外）
- op=-1 フレーム（`FutureCursor` 等）→ `LexiconSubscriptionException`（`Error.Error` = エラーコード, `Error.Message` = 説明）

### 2. IAtProtocolClient のメソッド

```csharp
// 旧 SendAsync を分割
ValueTask<LexiconResponse<TOutput>> QueryAsync<TOutput>(
    IQueryRequest request,
    IHttpResponseDeserializer<TOutput> responseDeserializer,
    CancellationToken cancellationToken = default);

ValueTask<LexiconResponse<TOutput>> ProcedureAsync<TOutput>(
    IProcedureRequest request,
    IHttpResponseDeserializer<TOutput> responseDeserializer,
    CancellationToken cancellationToken = default);

IAsyncEnumerable<TMessage> SubscribeAsync<TMessage>(
    ISubscribeRequest request,
    ISubscriptionMessageDeserializer<TMessage> messageDeserializer,
    CancellationToken cancellationToken = default);
```

### 3. AtProtocolClient WebSocket 実装

```csharp
public async IAsyncEnumerable<TMessage> SubscribeAsync<TMessage>(
    ISubscribeRequest request,
    ISubscriptionMessageDeserializer<TMessage> messageDeserializer,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var uri = BuildWebSocketUri(request); // http→ws / https→wss スキーム変換
    using var ws = new ClientWebSocket();
    await ws.ConnectAsync(uri, cancellationToken);

    var buffer = new List<byte>();
    while (!cancellationToken.IsCancellationRequested)
    {
        // フレームを buffer に受信
        // ParseCborFrame: ヘッダー(op/t)を読み、op=-1 なら LexiconSubscriptionException を throw
        // deserializer.Deserialize(t, payload) で TMessage? を得る
        // null でなければ yield return
    }
}
```

**CBOR フレームパースの仕様：**
- `System.Formats.Cbor.CborReader` でヘッダーを読む
- ヘッダー読了後の残バイト列がペイロード（`reader.BytesRemaining` で位置を計算）
- op=1: 通常メッセージ → `deserializer.Deserialize(t, payload)` に委譲
- op=-1: エラー → ペイロードから `error`/`message` を読んで `LexiconSubscriptionException` を throw

### 4. DocumentEmitter.EmitSubscription の修正

union schema の場合に `IMessage` マーカーインターフェースを生成し、各 union メンバーを `unionMemberImpls` に追加する：

```csharp
if (subDef.Message.Schema is UnionDefinition unionDef)
{
    // IMessage インターフェースファイルを生成
    EmitSubscriptionMessageInterface(nsid, segments, generatedCodeNamespace, files);

    // 各 union ref に対して (memberTypePath, "Xxx.IMessage") を登録
    // → 既存の Phase 2 EmitUnionMemberImpl がメンバークラスに `: IMessage {}` を付与
    var interfacePath = string.Join(".", segments) + ".IMessage";
    foreach (var refStr in unionDef.Refs)
    {
        var memberPath = LexiconNameHelper.ResolveRef(nsid, refStr, nsidIndex);
        unionMemberImpls.Add((memberPath, interfacePath));
    }
}
```

### 5. SubscriptionRequestEmitter 生成コード例

```csharp
[global::BlueBlaze.Core.Lexicon("com.atproto.sync.subscribeRepos",
    global::BlueBlaze.Core.LexiconOperationKind.Subscription)]
public sealed class Request : global::BlueBlaze.Core.ISubscribeRequest
{
    public Request(Parameters? parameters = null) { this.Parameters = parameters; }
    public string Nsid => "com.atproto.sync.subscribeRepos";
    public global::BlueBlaze.Core.ILexiconParameters? Parameters { get; }
}
```

### 6. SubscriptionDeserializerEmitter 生成コード例

CBOR の型マッピング：

| Lexicon 型 | CborReader 呼び出し |
|-----------|-------------------|
| `boolean` | `reader.ReadBoolean()` |
| `integer` | `(int)reader.ReadInt64()` |
| `string` | `reader.ReadTextString()` |
| `bytes` | `reader.ReadByteString().ToArray()` |
| `cid-link` | Tag 42 → `reader.ReadTag(); reader.ReadByteString()` → 暫定 `byte[]` |
| `array` of T | ループで各要素を読む補助メソッド |
| nullable フィールド | `reader.PeekState() == Null ? (reader.ReadNull(), null).Item2 : ...` |
| `ref` 型 | 対応する型の補助デシリアライズメソッドを生成して再帰呼び出し |
| 未知のキー | `reader.SkipValue()` |

```csharp
public sealed class Deserializer
    : global::BlueBlaze.Core.ISubscriptionMessageDeserializer<IMessage>
{
    public static readonly Deserializer Instance = new();
    private Deserializer() { }

    public IMessage? Deserialize(string? messageType, global::System.ReadOnlySpan<byte> payload)
        => messageType switch
        {
            "#commit"   => DeserializeCommit(payload),
            "#sync"     => DeserializeSync(payload),
            "#identity" => DeserializeIdentity(payload),
            "#account"  => DeserializeAccount(payload),
            "#info"     => DeserializeInfo(payload),
            _           => null
        };

    private static MessageCommit DeserializeCommit(global::System.ReadOnlySpan<byte> payload)
    {
        var reader = new global::System.Formats.Cbor.CborReader(payload, ...);
        // フィールドごとの switch + 手動パース
        // 最後に new MessageCommit(seq, rebase, ...) を返す
    }
    // ...
}
```

### 7. ClientMethodEmitter 生成コード例

```csharp
// Query の場合
public ValueTask<...> GetTimelineAsync(...) =>
    client.QueryAsync(new Request(parameters), Deserializer.Instance, ct);

// Procedure の場合
public ValueTask<...> CreatePostAsync(...) =>
    client.ProcedureAsync(new Request(input), Deserializer.Instance, ct);

// Subscription の場合
public IAsyncEnumerable<IMessage> SubscribeReposAsync(
    global::GeneratedNs.Com.Atproto.Sync.SubscribeRepos.Parameters? parameters = null,
    global::System.Threading.CancellationToken cancellationToken = default)
{
    return client.SubscribeAsync(
        new global::GeneratedNs.Com.Atproto.Sync.SubscribeRepos.Request(parameters),
        global::GeneratedNs.Com.Atproto.Sync.SubscribeRepos.Deserializer.Instance,
        cancellationToken);
}
```

### 8. LexiconCodeGenerator Phase 3 の修正

現在の `else { continue; }` を以下に置き換え：

```csharp
else if (mainDef is SubscriptionDefinition subDef)
{
    var hasParameters = subDef.Parameters?.Properties?.Count > 0;
    SubscriptionRequestEmitter.Emit(nsid, segments, hasParameters, generatedCodeNamespace, files);

    if (subDef.Message.Schema is UnionDefinition subscriptionUnionDef)
    {
        SubscriptionDeserializerEmitter.Emit(
            nsid, segments, subscriptionUnionDef, nsidIndex, defIndex,
            generatedCodeNamespace, files);
    }
}
```

`seenPrefixes` への追加も同様に行う（Phase 4 の prefix struct 生成のため）。

---

## netstandard2.0 対応

- `System.Formats.Cbor`: NuGet パッケージ参照が必要（`dotnet package add`）
- `Microsoft.Bcl.AsyncInterfaces`: `IAsyncEnumerable<T>` を netstandard2.0 で使うため必要（`dotnet package add`）
- `Microsoft.Bcl.Memory` または `System.Memory`: `ReadOnlySpan<byte>` のため（`Directory.Packages.props` にバージョン定義済み）
- `ClientWebSocket.ReceiveAsync(Memory<byte>, CT)` は .NET 5+ のみ → `#if NET` で `ArraySegment<byte>` 版にフォールバック

---

## 実装順序

作業を以下の4段階で進める。段階2の手書きコードが動作確認済みの「正解」として、段階3のジェネレータ実装を検証する基準になる。

### 段階1: コア定義

1. `LexiconOperationKind` に `Subscription` 追加
2. `LexiconError.Description` → `Message` にリネーム
3. `IResponseDeserializer` → `IHttpResponseDeserializer` に改名（`VoidOutputDeserializer`, `RawJsonDeserializer` も追従）
4. `LexiconException` を基底クラスに縮小、`LexiconHttpException` を新規作成
5. Core 新規インターフェース・例外クラスの追加（`IQueryRequest`, `IProcedureRequest`, `ISubscribeRequest`, `ISubscriptionMessageDeserializer`, `LexiconSubscriptionException`）
6. `IAtProtocolClient` / `AtProtocolClient` の拡張（`SendAsync` 分割、WebSocket + CBOR フレームパース実装）
7. NuGet 依存の追加（netstandard2.0 用: `System.Formats.Cbor`, `Microsoft.Bcl.AsyncInterfaces`）
8. `dotnet build` が通ること、既存テストが pass することを確認

### 段階2: `subscribeRepos` を手書き実装

`com.atproto.sync.subscribeRepos` を対象に手書きで実装する（union 型・Parameters・エラー定義がすべて含まれるため検証に最適）：

- `IMessage` マーカーインターフェース
- `MessageCommit`, `MessageSync`, `MessageIdentity`, `MessageAccount`, `MessageInfo` の各 POCO クラス（`IMessage` 実装）
- `Request : ISubscribeRequest`
- `Deserializer : ISubscriptionMessageDeserializer<IMessage>`（CBOR パース手書き）
- `SubscribeReposAsync` 拡張メソッド
- 実際の Bluesky エンドポイントへの接続スモークテスト（任意）

### 段階3: ジェネレータ実装

段階2の手書きコードを「正解」として、以下を実装・検証：

- `DeserializerEmitter` を `IHttpResponseDeserializer` に対応（既存の HTTP 側変更）
- `RequestEmitter` を `IQueryRequest` / `IProcedureRequest` に対応
- `DocumentEmitter.EmitSubscription` に `IMessage` インターフェース生成を追加
- `SubscriptionRequestEmitter` 新規作成
- `SubscriptionDeserializerEmitter` 新規作成（最も工数大）
- `ClientMethodEmitter` を `QueryAsync`/`ProcedureAsync`/`SubscribeAsync` 呼び出しに更新
- `LexiconCodeGenerator` Phase 3 に subscription 分岐を追加
- ジェネレータの出力が手書きコードと等価になることを確認（スナップショットテスト）

### 段階4: 手書きコードを削除してジェネレータベースに移行

- 段階2で書いた手書きコードを削除
- ジェネレータが同等のコードを生成することを確認
- 全テスト pass を確認

---

## 検証方法

1. `dotnet build` でビルドが通ることを確認
2. 既存の `LexiconGeneratorGenerateTest` が引き続き pass することを確認
3. subscription 用の新規テストを追加：
   - `subscribeRepos.json` を入力として `IMessage` インターフェースが生成されること
   - `MessageCommit : IMessage` の partial class が生成されること
   - `Request : ISubscribeRequest` が生成されること
   - `Deserializer : ISubscriptionMessageDeserializer<IMessage>` が生成されること
   - `SubscribeReposAsync` 拡張メソッドが生成されること
4. （可能であれば）実際の Bluesky WebSocket エンドポイントへの接続をスモークテスト

---

## 既知の制約・スコープ外

- `cid-link` は暫定的に `byte[]` でデシリアライズ（後で CID 正規表現へ改善）
- 自動再接続は未実装（切断で `IAsyncEnumerable` が終了）
- `message.schema.type == "object"` のケース（現実には存在しないが仕様上は有効）は一旦スキップ
- `JsonSerializerContextEmitter`（Phase 6）への subscription 型の追加は今回スコープ外
