# Data Model: AtProtocol・Bluesky 二層クライアント ライブラリ

**Feature**: [spec.md](spec.md)
**Date**: 2026-01-01
**Purpose**: Phase 1 データモデル定義 - エンティティ、関係、バリデーションルール

## モデル概要

二層アーキテクチャに従い、モデルを2つのレイヤーに分離：

- **AtProtocol層**: プロトコル基盤（XRPC、DID、認証、エラー処理）
- **Bluesky層**: Bluesky固有のLexicon型（Post、Profile、Follow）

---

## AtProtocol層モデル

### 1. AtProtocolClient

**説明**: 低レベルAtProtocol操作の中心エントリポイント

**プロパティ**:
- `ServiceUrl` (Uri): PDSサーバーURL（例: `https://bsky.social`）
- `AuthProvider` (IAuthenticationProvider): 認証プロバイダー（OAuth or AppPassword）
- `Session` (Session?): 現在の認証セッション（nullの場合は未認証）
- `HttpClient` (HttpClient): 内部HTTPクライアント（DI注入）

**メソッド**:
- `AuthenticateAsync(IAuthenticationProvider)` → `Session`: 認証実行
- `QueryAsync<T>(string nsid, params)` → `T`: XRPCクエリ（GET）
- `ProcedureAsync<TInput, TOutput>(string nsid, TInput)` → `TOutput`: XRPCプロシージャ（POST）
- `ResolveDid(string did)` → `DIDDocument`: DID解決

**関係**:
- 1対1 `IAuthenticationProvider`（コンポジション）
- 1対0..1 `Session`（認証状態）

**バリデーション**:
- `ServiceUrl`: 必須、HTTPS URLのみ許可
- `QueryAsync`/`ProcedureAsync`: `nsid` はLexicon NSID形式（`com.atproto.*` or `app.bsky.*`）

---

### 2. XRPCRequest / XRPCResponse

**説明**: XRPC通信の低レベル表現

#### XRPCRequest
```csharp
public record XRPCRequest(
    string Nsid,                     // Lexicon NSID (例: "com.atproto.server.getSession")
    HttpMethod Method,                // GET or POST
    Dictionary<string, string>? Params, // クエリパラメータ（GETの場合）
    object? Body                      // リクエストボディ（POSTの場合）
);
```

#### XRPCResponse<T>
```csharp
public record XRPCResponse<T>(
    T Data,                          // レスポンスボディ
    HttpStatusCode StatusCode,       // HTTPステータス
    Dictionary<string, string> Headers // レスポンスヘッダー（Rate-Limit等）
);
```

**バリデーション**:
- `Nsid`: 正規表現 `^[a-z][a-z0-9-]*(\.[a-z0-9-]+)+$`
- `Method`: GET or POST のみ
- `Body`: POSTの場合は必須、GETの場合はnull

---

### 3. DIDDocument

**説明**: DID解決結果

```csharp
public record DIDDocument(
    string Id,                            // DID (例: "did:plc:abc123")
    List<string> AlsoKnownAs,             // ハンドル（例: ["at://alice.bsky.social"]）
    List<ServiceEndpoint> Service,        // PDSエンドポイント
    List<VerificationMethod> VerificationMethod // 公開鍵
);

public record ServiceEndpoint(
    string Id,                            // サービスID（例: "#atproto_pds"）
    string Type,                          // サービス種別（例: "AtprotoPersonalDataServer"）
    string ServiceEndpoint                // URL（例: "https://bsky.social"）
);

public record VerificationMethod(
    string Id,                            // 鍵ID
    string Type,                          // 鍵種別（例: "Multikey"）
    string Controller,                    // コントローラーDID
    string PublicKeyMultibase             // 公開鍵（Base58エンコード）
);
```

**バリデーション**:
- `Id`: `did:plc:` または `did:web:` で始まる
- `Service`: 少なくとも1つの `AtprotoPersonalDataServer` エントリが必要

**状態遷移**: なし（イミュータブル）

---

### 4. LexiconSchema

**説明**: Lexiconスキーマ定義（Source Generator入力）

```csharp
public record LexiconSchema(
    string Nsid,                     // 例: "app.bsky.feed.post"
    string Type,                     // "record" | "query" | "procedure"
    Dictionary<string, PropertyDef> Properties, // フィールド定義
    List<string> Required            // 必須フィールド名
);

public record PropertyDef(
    string Type,                     // "string" | "integer" | "datetime" | "ref" | "array" ...
    string? Format,                  // 日時フォーマット等
    int? MaxLength,                  // 文字列最大長
    string? Ref                      // 参照型（例: "com.atproto.repo.strongRef"）
);
```

**用途**: Source Generatorがこの定義からC#クラスを生成（開発者は直接使用しない）

---

### 5. GeneratedLexiconTypes (Source Generator出力)

**説明**: ビルド時にLexiconSchemaから生成される型

**例（app.bsky.feed.post）**:
```csharp
// 自動生成コード（BlueBlaze.Bluesky.Generated名前空間）
[JsonSourceGeneration(typeof(AppBskyFeedPost))]
public partial class AppBskyFeedPost
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTime CreatedAt { get; init; }

    [JsonPropertyName("embed")]
    public Embed? Embed { get; init; }

    [JsonPropertyName("facets")]
    public List<Facet>? Facets { get; init; }
}
```

**バリデーション**:
- Source Generatorが`[Required]`属性を生成
- System.Text.Jsonが逆シリアライズ時に必須チェック
- カスタムバリデーションは`IValidatableObject`実装

---

### 6. IAuthenticationProvider / 実装クラス

**説明**: 認証方式の抽象化

#### インターフェース
```csharp
public interface IAuthenticationProvider
{
    Task<Session> AuthenticateAsync(CancellationToken ct = default);
    Task<Session> RefreshAsync(Session current, CancellationToken ct = default);
}
```

#### AppPasswordAuth
```csharp
public record AppPasswordAuthConfig(
    string Identifier,    // ハンドルまたはDID
    string AppPassword    // アプリパスワード
);

public class AppPasswordAuth : IAuthenticationProvider
{
    private readonly AppPasswordAuthConfig _config;
    private readonly HttpClient _httpClient;

    // com.atproto.server.createSession呼び出し
    public async Task<Session> AuthenticateAsync(CancellationToken ct = default)
    {
        // XRPC: POST /xrpc/com.atproto.server.createSession
        // Body: { identifier, password }
        // Response: { accessJwt, refreshJwt, handle, did }
    }

    // com.atproto.server.refreshSession呼び出し
    public async Task<Session> RefreshAsync(Session current, CancellationToken ct = default)
    {
        // XRPC: POST /xrpc/com.atproto.server.refreshSession
        // Header: Authorization: Bearer <refreshJwt>
    }
}
```

#### OAuthDPoPAuth
```csharp
public record OAuthDPoPConfig(
    string ClientId,
    Uri RedirectUri,
    List<string> Scopes
);

public class OAuthDPoPAuth : IAuthenticationProvider
{
    private readonly OAuthDPoPConfig _config;
    private readonly DPoPProofGenerator _dpopGenerator;
    private readonly OidcClient _oidcClient; // IdentityModel.OidcClient

    public async Task<Session> AuthenticateAsync(CancellationToken ct = default)
    {
        // 1. DID解決で認可サーバー発見
        // 2. PAR (Pushed Authorization Request)
        // 3. 認可リダイレクト
        // 4. トークン交換（DPoPヘッダー付き）
        // 5. Session作成
    }
}
```

**バリデーション**:
- `AppPasswordAuthConfig.Identifier`: 空文字列不可
- `AppPasswordAuthConfig.AppPassword`: 最低16文字
- `OAuthDPoPConfig.RedirectUri`: HTTPS推奨

---

### 7. Session

**説明**: 認証後のセッション情報

```csharp
public record Session(
    string AccessToken,          // JWT or DPoPトークン
    string RefreshToken,         // リフレッシュトークン
    DateTime ExpiresAt,          // アクセストークン有効期限
    string Did,                  // ユーザーDID
    string Handle,               // ユーザーハンドル（例: "alice.bsky.social"）
    SessionMetadata Metadata     // 追加メタデータ
);

public record SessionMetadata(
    AuthenticationType Type,     // OAuth or AppPassword
    ECDsa? DPoPKey               // OAuth DPoPの場合のみ、秘密鍵
);

public enum AuthenticationType
{
    AppPassword,
    OAuthDPoP
}
```

**状態遷移**:
1. **未認証** → `AuthenticateAsync()` → **認証済み**
2. **認証済み** → トークン有効期限切れ → **リフレッシュ必要**
3. **リフレッシュ必要** → `RefreshAsync()` → **認証済み**
4. **認証済み** → ログアウト → **未認証**

**バリデーション**:
- `ExpiresAt`: 現在時刻より未来であること
- `Did`: `did:plc:` or `did:web:` 形式

**セキュリティ**:
- トークンはメモリ上でのみ保持（永続化時は暗号化）
- `DPoPKey`はIDisposableで安全に破棄

---

### 8. AtProtocolException（例外階層）

**説明**: 構造化エラー情報

#### 基底クラス
```csharp
public class AtProtocolException : Exception
{
    public string ErrorCode { get; init; }        // 例: "InvalidToken"
    public bool IsRetryable { get; init; }        // リトライ可能か
    public Dictionary<string, object> Details { get; init; } // 詳細情報

    public AtProtocolException(
        string errorCode,
        string message,
        bool isRetryable = false,
        Dictionary<string, object>? details = null,
        Exception? innerException = null
    ) : base(message, innerException)
    {
        ErrorCode = errorCode;
        IsRetryable = isRetryable;
        Details = details ?? new();
    }
}
```

#### 派生クラス
```csharp
public class AuthenticationException : AtProtocolException
{
    // IsRetryable = false
    // ErrorCode: "InvalidToken", "ExpiredToken", "AuthRequired"
}

public class NetworkException : AtProtocolException
{
    // IsRetryable = true
    // ErrorCode: "NetworkError", "Timeout", "ServiceUnavailable"
}

public class ValidationException : AtProtocolException
{
    // IsRetryable = false
    // ErrorCode: "InvalidRequest", "InvalidRecord"
    public Dictionary<string, List<string>> FieldErrors { get; init; }
}

public class RateLimitException : AtProtocolException
{
    // IsRetryable = true
    // ErrorCode: "RateLimitExceeded"
    public DateTime? RetryAfter { get; init; } // Rate-Limit-Resetヘッダーから解析
}
```

**用途**:
```csharp
try
{
    await client.ProcedureAsync(...);
}
catch (RateLimitException ex)
{
    if (ex.RetryAfter.HasValue)
    {
        await Task.Delay(ex.RetryAfter.Value - DateTime.UtcNow);
        // リトライ
    }
}
catch (AuthenticationException)
{
    // 再認証が必要
}
```

---

### 9. RetryPolicy

**説明**: 自動リトライロジック（Polly統合）

```csharp
public record RetryPolicyConfig(
    int MaxRetries = 3,              // 最大リトライ回数
    TimeSpan InitialDelay = 100ms,   // 初回遅延
    TimeSpan MaxDelay = 5s,          // 最大遅延
    double BackoffMultiplier = 2.0   // 指数バックオフ係数
);

public class RetryPolicy
{
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public RetryPolicy(RetryPolicyConfig config)
    {
        _policy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r =>
                r.StatusCode >= HttpStatusCode.InternalServerError ||
                r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                config.MaxRetries,
                attempt => TimeSpan.FromMilliseconds(
                    Math.Min(
                        config.InitialDelay.TotalMilliseconds * Math.Pow(config.BackoffMultiplier, attempt),
                        config.MaxDelay.TotalMilliseconds
                    )
                )
            );
    }

    public Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> action) =>
        _policy.ExecuteAsync(action);
}
```

---

## Bluesky層モデル

### 10. BlueskyClient

**説明**: Bluesky固有の操作を提供

**プロパティ**:
- `AtProto` (AtProtocolClient): 内部のAtProtocol層クライアント
- `Posts` (PostService): 投稿操作
- `Profiles` (ProfileService): プロフィール操作
- `Follows` (FollowService): フォロー操作

**メソッド**:
```csharp
public class BlueskyClient
{
    private readonly AtProtocolClient _atproto;

    public BlueskyClient(AtProtocolClient atproto)
    {
        _atproto = atproto;
        Posts = new PostService(_atproto);
        Profiles = new ProfileService(_atproto);
        Follows = new FollowService(_atproto);
    }

    public PostService Posts { get; }
    public ProfileService Profiles { get; }
    public FollowService Follows { get; }
}
```

**関係**:
- 1対1 `AtProtocolClient`（委譲）
- 1対1 各Service（コンポジション）

---

### 11. Post（Bluesky Lexiconエンティティ）

**説明**: app.bsky.feed.post Lexicon型（Source Generator生成）

```csharp
// 生成コード例
public partial record Post
{
    [JsonPropertyName("$type")]
    public string Type => "app.bsky.feed.post";

    [JsonPropertyName("text")]
    [MaxLength(300)]
    public required string Text { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTime CreatedAt { get; init; }

    [JsonPropertyName("embed")]
    public Embed? Embed { get; init; }

    [JsonPropertyName("facets")]
    public List<Facet>? Facets { get; init; }

    [JsonPropertyName("langs")]
    public List<string>? Langs { get; init; }
}

public record Embed(...); // 画像、外部リンク等
public record Facet(...); // メンション、リンク等のテキストアノテーション
```

**バリデーション**:
- `Text`: 1～300文字
- `CreatedAt`: 過去の日時のみ許可（将来の投稿は無効）
- `Langs`: ISO 639-1言語コード

**状態遷移**: イミュータブル（作成後は変更不可）

---

### 12. Profile（Bluesky Lexiconエンティティ）

**説明**: app.bsky.actor.profile Lexicon型

```csharp
public partial record Profile
{
    [JsonPropertyName("$type")]
    public string Type => "app.bsky.actor.profile";

    [JsonPropertyName("displayName")]
    [MaxLength(64)]
    public string? DisplayName { get; init; }

    [JsonPropertyName("description")]
    [MaxLength(256)]
    public string? Description { get; init; }

    [JsonPropertyName("avatar")]
    public Blob? Avatar { get; init; } // 画像Blob参照

    [JsonPropertyName("banner")]
    public Blob? Banner { get; init; }
}

public record Blob(string Cid, string MimeType); // IPFS CID
```

**バリデーション**:
- `DisplayName`: 最大64文字
- `Description`: 最大256文字
- `Avatar`/`Banner`: 画像MIMEタイプのみ（image/png, image/jpeg）

---

### 13. Follow（Bluesky Lexiconエンティティ）

**説明**: app.bsky.graph.follow Lexicon型

```csharp
public partial record Follow
{
    [JsonPropertyName("$type")]
    public string Type => "app.bsky.graph.follow";

    [JsonPropertyName("subject")]
    public required string Subject { get; init; } // フォロー対象のDID

    [JsonPropertyName("createdAt")]
    public required DateTime CreatedAt { get; init; }
}
```

**バリデーション**:
- `Subject`: 有効なDID形式
- 自分自身のDIDは不可（セルフフォロー禁止）

---

## エンティティ関係図

```
┌─────────────────────┐
│ AtProtocolClient    │
│  (AtProtocol層)      │
├─────────────────────┤
│ + ServiceUrl        │
│ + AuthProvider ────┼──▶ IAuthenticationProvider
│ + Session          │         │
│                     │         ├──▶ AppPasswordAuth
│ + QueryAsync()      │         └──▶ OAuthDPoPAuth
│ + ProcedureAsync()  │
│ + ResolveDid() ────┼──▶ DIDDocument
└─────────────────────┘
        △
        │ 委譲
        │
┌───────┴─────────────┐
│ BlueskyClient       │
│  (Bluesky層)        │
├─────────────────────┤
│ + Posts ───────────┼──▶ PostService ──▶ Post (Generated)
│ + Profiles ────────┼──▶ ProfileService ──▶ Profile (Generated)
│ + Follows ─────────┼──▶ FollowService ──▶ Follow (Generated)
└─────────────────────┘

例外階層:
AtProtocolException
 ├── AuthenticationException
 ├── NetworkException
 ├── ValidationException
 └── RateLimitException
```

---

## バリデーションルール一覧

| エンティティ | フィールド | ルール |
|------------|----------|--------|
| AtProtocolClient | ServiceUrl | 必須、HTTPS URL |
| XRPCRequest | Nsid | Lexicon NSID形式 |
| DIDDocument | Id | `did:plc:` or `did:web:` |
| AppPasswordAuthConfig | AppPassword | 最低16文字 |
| Session | ExpiresAt | 現在時刻より未来 |
| Post | Text | 1～300文字 |
| Post | CreatedAt | 過去の日時のみ |
| Profile | DisplayName | 最大64文字 |
| Profile | Description | 最大256文字 |
| Follow | Subject | 有効なDID、自分自身不可 |

---

## Source Generator生成戦略

1. **入力**: `/lexicons/**/*.json` ディレクトリのLexicon定義
2. **処理**: Source Generatorが各Lexiconファイルを解析
3. **出力**: `BlueBlaze.Generated` 名前空間に`partial class`生成
4. **JsonSerializerContext**: すべての生成型を登録
5. **AOT対応**: `[JsonSourceGeneration]` 属性でリフレクション排除

**生成例**:
```
lexicons/
├── com/atproto/server/createSession.json
├── app/bsky/feed/post.json
├── app/bsky/actor/profile.json
└── app/bsky/graph/follow.json

↓ Source Generator

BlueBlaze.AtProtocol.Generated/
├── ComAtprotoServerCreateSession.g.cs
└── ...

BlueBlaze.Bluesky.Generated/
├── AppBskyFeedPost.g.cs
├── AppBskyActorProfile.g.cs
└── AppBskyGraphFollow.g.cs
```

---

## Phase 2 (tasks.md) 移行準備完了

すべてのエンティティ、関係、バリデーションルールが定義されました。次のフェーズで、これらのモデルを実装するタスクを分解します。
