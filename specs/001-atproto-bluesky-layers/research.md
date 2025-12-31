# Research: AtProtocol・Bluesky 二層クライアント ライブラリ

**Feature**: [spec.md](spec.md)
**Date**: 2026-01-01
**Purpose**: Phase 0 技術調査 - 実装に必要な技術選定と設計パターンの決定

## 1. AtProtocol仕様とLexicon型システム

### 決定: Lexicon定義からのSource Generator生成 + System.Text.Json

**調査内容**:
- AtProtocol公式仕様: https://atproto.com/specs/lexicon
- Lexiconはスキーマ定義言語で、各API操作のリクエスト/レスポンス型を定義
- 公式TypeScript実装では、Lexicon JSONから型を生成するコード生成器を使用
- .NETでは、Source Generatorによりビルド時に型生成が可能

**選択理由**:
- **コンパイル時型安全性**: Source Generatorで生成された型により、不正な操作はコンパイルエラーとなる（SC-005要件）
- **AOT互換性**: System.Text.JsonのSource Generatorモードは、NativeAOTと完全互換
- **パフォーマンス**: リフレクションを使わないため、起動時間とランタイムパフォーマンスが向上
- **開発者体験**: IntelliSenseで型補完が効き、20行/30行以下のコード目標（SC-001, SC-002）を達成しやすい

**代替案検討**:
- ❌ **手動型定義**: Lexiconスキーマ更新時の同期コストが高く、ヒューマンエラーのリスク
- ❌ **Newtonsoft.Json**: AOT非対応、リフレクションベース
- ❌ **動的型（JObject）**: 型安全性なし、SC-005要件を満たせない

**実装方針**:
- `BlueBlaze.AtProtocol.SourceGenerator` プロジェクトを作成
- Lexicon JSON定義を `/lexicons/` ディレクトリに配置
- Source Generatorが `AdditionalFiles` から読み込み、`partial class` として型生成
- `JsonSerializerContext` パターンで AOT対応を保証

**参考実装**:
- 公式TypeScript SDK: https://github.com/bluesky-social/atproto/tree/main/packages/api
- .NET Source Generator ガイド: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview

---

## 2. OAuth 2.1 + DPoP認証実装

### 決定: IdentityModel.OidcClient ベース + 手動DPoP実装

**調査内容**:
- AtProtocol OAuth仕様: https://atproto.com/specs/oauth
- DPoP (Demonstrating Proof of Possession) RFC 9449: https://datatracker.ietf.org/doc/html/rfc9449
- PAR (Pushed Authorization Requests) RFC 9126: https://datatracker.ietf.org/doc/html/rfc9126
- PKCE RFC 7636: https://datatracker.ietf.org/doc/html/rfc7636

**選択理由**:
- **IdentityModel.OidcClient**: PKCE, PAR などのOAuth 2.1基本機能を提供、NuGetで利用可能
- **手動DPoP実装**: .NETにDPoP専用ライブラリがないため、ECDsa鍵ペア生成とJWS署名を自前実装
- **System.Security.Cryptography**: .NET標準ライブラリでES256署名対応、AOT互換

**代替案検討**:
- ❌ **IdentityServer**: サーバー側ライブラリで用途が異なる
- ❌ **Duende.AccessTokenManagement**: DPoP非対応
- ❌ **完全自作OAuth**: 複雑度が高く、既存ライブラリの恩恵を受けられない

**実装方針**:
- `OAuthHandler` クラスで認可フロー全体を管理
- `DPoPProofGenerator` クラスでJWS署名生成（ES256アルゴリズム）
- 鍵ペアは `ECDsa.Create()` で生成、JWKフォーマットでシリアライズ
- `SessionManager` でトークン保存と自動リフレッシュ
- HTTPリクエストごとにDPoPヘッダーを動的生成（`jti`, `htm`, `htu`含む）

**セキュリティ考慮**:
- トークンは `System.Security.Cryptography.ProtectedData` で暗号化保存（Windows）
- Linux/macOSでは `~/.config` にファイルベース保存、パーミッション 600
- PKCE `code_verifier` はCSPRNGで生成（`RandomNumberGenerator.GetBytes(32)`）

---

## 3. App Passwords認証実装

### 決定: com.atproto.server.createSession XRPC呼び出し

**調査内容**:
- App Passwords仕様: https://atproto.com/specs/xrpc#com-atproto-server-createsession
- 公式Blueskyアプリ設定でApp Password生成可能
- セッショントークンは `accessJwt` と `refreshJwt` のペア

**選択理由**:
- **シンプルさ**: OAuth 2.1より実装が簡単で、テスト・デモ用途に適する
- **後方互換性**: 既存のBlueskyユーザーが即座に利用可能
- **抽象化**: `AuthenticationProvider` インターフェースで OAuth と統一的に扱える

**実装方針**:
- `AppPasswordAuth` クラス: identifier（ハンドルまたはDID）とapp-passwordを受け取る
- `com.atproto.server.createSession` をXRPC POST
- レスポンスの `accessJwt` を `Authorization: Bearer` で使用
- `refreshJwt` で `com.atproto.server.refreshSession` 呼び出し

**非推奨化対応**:
- 将来、AtProtocolがApp Passwordsを廃止する可能性に備え、OAuth優先を推奨
- ドキュメントで「OAuth 2.1推奨、App Passwordsはレガシー」と明記

---

## 4. XRPCクライアント実装

### 決定: HttpClient + Polly（リトライ）+ System.Text.Json

**調査内容**:
- XRPC仕様: https://atproto.com/specs/xrpc
- HTTPメソッド: query=GET, procedure=POST
- Content-Type: application/json
- エラーレスポンス: `{error: string, message: string}`

**選択理由**:
- **HttpClient**: .NET標準、`IHttpClientFactory`でDI対応、AOT互換
- **Polly**: リトライ、サーキットブレーカー、タイムアウトをポリシーベースで実装（FR-008自動リトライ要件）
- **System.Text.Json**: 既に選定済み、Source Generatorで一貫性

**実装方針**:
- `XrpcClient` 基底クラス: `QueryAsync<T>`, `ProcedureAsync<TInput, TOutput>` メソッド
- Pollyポリシー:
  - **リトライ**: 一時的なネットワークエラー（5xx, タイムアウト）で最大3回、指数バックオフ
  - **タイムアウト**: 2秒（SC-006要件）
  - **サーキットブレーカー**: 5回連続失敗で30秒間オープン
- DPoP/Bearerトークンを `HttpRequestMessage.Headers` に自動注入
- エラーレスポンスを `AtProtocolException` 派生クラスにマッピング

**エラーハンドリング**:
```csharp
// エラーコード → 例外クラスマッピング
"InvalidToken" → AuthenticationException (IsRetryable=false)
"RateLimitExceeded" → RateLimitException (IsRetryable=true, RetryAfter解析)
"InvalidRequest" → ValidationException (IsRetryable=false)
[5xx] → NetworkException (IsRetryable=true)
```

---

## 5. DID解決実装

### 決定: /.well-known/did.json + did:plc解決

**調査内容**:
- DID仕様: https://atproto.com/specs/did
- AtProtocolは `did:plc` と `did:web` をサポート
- `did:plc` は PLC Directory（https://plc.directory）で解決
- `did:web` は `/.well-known/did.json` から取得

**実装方針**:
- `DidResolver` クラス: `ResolveAsync(string did)` → `DIDDocument`
- `did:plc:xxxxx` → `GET https://plc.directory/{did}`
- `did:web:example.com` → `GET https://example.com/.well-known/did.json`
- HTTPキャッシュ: 24時間（`CacheControlHeaderValue.MaxAge`）
- DIDドキュメントから `alsoKnownAs`（ハンドル）と `service`（PDS URL）を抽出

---

## 6. NuGetパッケージング戦略

### 決定: 2つの独立パッケージ + SourceLink + Deterministic Build

**パッケージ構成**:
```
BlueBlaze.AtProtocol (v1.0.0)
├── 依存関係:
│   ├── System.Text.Json (8.0+)
│   ├── Microsoft.Extensions.Http.Polly (8.0+)
│   └── IdentityModel.OidcClient (6.0+)
└── 含むアセンブリ:
    ├── BlueBlaze.AtProtocol.dll
    └── BlueBlaze.AtProtocol.SourceGenerator.dll (Analyzer)

BlueBlaze.Bluesky (v1.0.0)
├── 依存関係:
│   └── BlueBlaze.AtProtocol (1.0.0+)
└── 含むアセンブリ:
    └── BlueBlaze.Bluesky.dll
```

**NuGet設定（.csprojメタデータ）**:
```xml
<PropertyGroup>
  <PackageId>BlueBlaze.AtProtocol</PackageId>
  <Version>1.0.0</Version>
  <Authors>BlueBlaze Contributors</Authors>
  <Description>AtProtocol client library for .NET</Description>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/your-org/BlueBlaze</PackageProjectUrl>
  <RepositoryUrl>https://github.com/your-org/BlueBlaze.git</RepositoryUrl>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <Deterministic>true</Deterministic>
  <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
</PropertyGroup>
```

**ビルドコマンド**:
```bash
dotnet pack --configuration Release -p:ContinuousIntegrationBuild=true
```

---

## 7. AOT対応戦略

### 決定: NativeAOT PublishTrimmed + IL Linker対応

**要件**:
- FR-011: .NET実装
- ユーザー指示: AOTコンパイル対応

**対応内容**:
1. **JsonSerializerContext使用**: すべてのLexicon型をSource Generatorで登録
2. **リフレクション排除**: `typeof()`, `Activator.CreateInstance()` を使用しない
3. **動的コード生成排除**: `Expression.Compile()`, `Reflection.Emit` 使用禁止
4. **警告対応**: `<PublishAot>true</PublishAot>`でビルドし、IL3050等の警告をゼロにする

**検証方法**:
```xml
<!-- プロジェクトファイル -->
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <IsAotCompatible>true</IsAotCompatible>
  <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
</PropertyGroup>
```

**ビルドテスト**:
```bash
dotnet publish -r linux-x64 -c Release /p:PublishAot=true
```

**既知の制約**:
- IdentityModel.OidcClient: 一部リフレクション使用 → トリミング警告を個別抑制
- Polly: v8.xでAOT対応済み → 問題なし

---

## 8. BDDテスト戦略（Reqnroll + 日本語）

### 決定: Reqnroll 2.x + TUnit + 日本語.feature

**調査内容**:
- Reqnroll: SpecFlowのオープンソース後継、.NET 8+対応
- TUnit: 高速な.NETテストフレームワーク、NativeAOT対応
- 日本語featureファイル: Gherkin仕様で完全サポート

**プロジェクト構成**:
```
tests/
├── BlueBlaze.AtProtocol.Tests/        # TUnit単体・統合テスト
│   └── XrpcClientTests.cs
└── BlueBlaze.Specs/                    # Reqnroll BDD
    ├── Features/
    │   ├── 001_XRPC呼び出し.feature
    │   ├── 002_OAuth認証.feature
    │   └── 003_投稿操作.feature
    └── StepDefinitions/
        └── XrpcSteps.cs
```

**Reqnroll設定（reqnroll.json）**:
```json
{
  "language": {
    "feature": "ja-JP"
  },
  "bindingCulture": {
    "name": "ja-JP"
  }
}
```

**サンプル.feature**:
```gherkin
# language: ja
機能: XRPC呼び出し
  AtProtocol層を使用して任意のXRPCリクエストを送信できる

  シナリオ: 正常なXRPCクエリ実行
    前提 有効なAtProtocolサーバーURLが設定されている
    もし 開発者がXRPCクエリを実行する
    ならば 正しいレスポンスが返される
```

**BDDサイクル厳守**:
1. **Red**: .featureファイル作成 → ステップ定義未実装でテスト失敗
2. **Green**: ステップ定義とプロダクションコード実装 → テスト成功
3. **Refactor**: コード整理 + `dotnet format` + 警告ゼロ確認

---

## 9. CI/CDパイプライン（GitHub Actions）

### 決定: マルチジョブ並列実行 + NuGet自動発行

**ワークフロー構成**:
```yaml
# .github/workflows/ci.yml
name: CI

on: [push, pull_request]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore --configuration Release
      - run: dotnet test --no-build --configuration Release --logger trx
      - run: dotnet format --verify-no-changes

  aot-validation:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet publish src/BlueBlaze.AtProtocol -r linux-x64 -c Release /p:PublishAot=true
      - run: dotnet publish src/BlueBlaze.Bluesky -r linux-x64 -c Release /p:PublishAot=true

  publish-nuget:
    if: github.ref == 'refs/heads/main'
    needs: [build-and-test, aot-validation]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet pack --configuration Release -p:ContinuousIntegrationBuild=true
      - run: dotnet nuget push **/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

**憲法原則VI準拠**:
- ✅ すべてのコミットがCIを通過
- ✅ 自動ビルド、テスト、フォーマット検証
- ✅ AOT互換性の自動検証
- ✅ mainブランチへのマージ後、自動NuGet発行

---

## 10. パフォーマンス目標達成戦略

### 目標: SC-006「2秒以内のAPI操作完了」

**測定方法**:
- TUnitベンチマーク: `[Benchmark]` 属性で測定
- BenchmarkDotNet統合（詳細プロファイリング用）

**最適化ポイント**:
1. **HTTP接続再利用**: `HttpClient`を`IHttpClientFactory`経由でシングルトン化
2. **JSON最適化**: Source Generator（リフレクション排除）
3. **並列化**: 複数XRPC呼び出しを`Task.WhenAll`で並列実行可能
4. **キャッシュ**: DID解決結果を24時間キャッシュ
5. **Pollyタイムアウト**: デフォルト2秒、調整可能

**ベンチマーク目標値**:
```
com.atproto.server.getSession (認証済み): <100ms (p95)
app.bsky.feed.post (投稿作成): <300ms (p95)
app.bsky.actor.getProfile: <150ms (p95)
DID解決（キャッシュミス時）: <500ms (p95)
```

---

## 研究成果サマリー

| 領域 | 選択技術 | 主要な理由 |
|------|---------|----------|
| 言語/ランタイム | .NET 10.0, C# 14 | AOT対応、Source Generator、NuGet生態系 |
| JSON処理 | System.Text.Json (Source Generator) | AOT互換、高速、型安全 |
| Lexicon型生成 | Source Generator | コンパイル時型チェック、SC-005達成 |
| OAuth 2.1 | IdentityModel.OidcClient + 手動DPoP | PKCE/PAR対応、DPoPは自前実装 |
| HTTPクライアント | HttpClient + Polly | 標準ライブラリ、リトライ/タイムアウト |
| BDDテスト | Reqnroll + TUnit + 日本語 | 憲法原則II準拠、.NET 10対応 |
| パッケージング | 2つのNuGetパッケージ | FR-001準拠、独立ビルド可能 |
| CI/CD | GitHub Actions | 憲法原則VI準拠、自動NuGet発行 |

**Phase 1移行準備完了**: すべてのNEEDS CLARIFICATIONが解決され、技術スタックが確定しました。
