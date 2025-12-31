# Implementation Plan: AtProtocol・Bluesky 二層クライアント ライブラリ

**Branch**: `001-atproto-bluesky-layers` | **Date**: 2026-01-01 | **Spec**: [spec.md](spec.md)

## Summary

既存のBlueskyサーバーに接続して操作を行う再利用可能な.NETクライアントライブラリを実装します。AtProtocol層（低レベルXRPC、DID解決、Lexiconスキーマ処理）とBluesky層（app.bsky.* Lexicon実装）の二層構造を採用し、将来の他のAtProtocolアプリケーションでも再利用可能な設計とします。

**技術アプローチ**:
- **Source Generator**によるLexicon型の静的生成で型安全性を保証（SC-005）
- **OAuth 2.1 + DPoP** および **App Passwords** 両方の認証方式をサポート
- **構造化エラー処理**と**自動リトライ**（Polly）により回復力を強化
- **NativeAOT対応**（System.Text.Json Source Generator、リフレクション排除）
- **BDD（Reqnroll + 日本語.feature）**による厳格なテストファースト開発
- **2つのNuGetパッケージ**（BlueBlaze.AtProtocol、BlueBlaze.Bluesky）で独立性を確保

---

## Technical Context

**Language/Version**: .NET 10.0, C# 14
**Primary Dependencies**: System.Text.Json (8.0+), Microsoft.Extensions.Http.Polly (8.0+), IdentityModel.OidcClient (6.0+), Reqnroll (2.x), TUnit (1.x)
**Storage**: N/A（クライアントライブラリ）
**Testing**: Reqnroll (BDD、日本語.feature)、TUnit (単体・統合)、BenchmarkDotNet (パフォーマンス)
**Target Platform**: .NET 10.0ランタイム、NativeAOT対応
**Project Type**: ライブラリ（NuGetパッケージ × 2）
**Performance Goals**: API操作 <2秒（p95）、DID解決 <500ms、投稿作成 <300ms
**Constraints**: AOT互換性、メモリ <50MB、型安全性（コンパイルエラー必須）
**Scale/Scope**: Lexicon型 30～50、コード行数 5,000～8,000行

---

## Constitution Check

### 原則I: コード品質とメンテナンス性
✅ **COMPLIANT** - Fluent API、2つのNuGetパッケージ、Source Generator、インターフェース抽象化

### 原則II: Behavior Driven Development
✅ **COMPLIANT** - Reqnroll + 日本語.feature、Red → Green → Refactor、警告ゼロ、CI/CD自動検証

### 原則III: テスト可能性
✅ **COMPLIANT** - DI対応、単一責任、80%カバレッジ、単体・統合・契約テスト

### 原則IV: セキュリティ
✅ **COMPLIANT** - トークン暗号化、PKCE、DPoP、入力検証、セキュリティレビュー

### 原則V: 可観測性
✅ **COMPLIANT** - ILogger構造化ログ、例外詳細情報、SourceLink

### 原則VI: クラウドネイティブとCI
✅ **COMPLIANT** - ステートレス、GitHub Actions CI/CD、自動NuGet発行

**総合判定**: ✅ **ALL GATES PASSED**

---

## Project Structure

### Documentation

```text
specs/001-atproto-bluesky-layers/
├── spec.md
├── plan.md (this file)
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── atprotocol-api.yaml
│   └── bluesky-api.yaml
└── checklists/
```

### Source Code

```text
src/
├── BlueBlaze.AtProtocol/           # NuGetパッケージ1
│   ├── AtProtocolClient.cs
│   ├── Auth/
│   ├── Xrpc/
│   ├── Did/
│   ├── Lexicon/
│   ├── Exceptions/
│   └── Policies/
├── BlueBlaze.AtProtocol.SourceGenerator/
│   └── LexiconSourceGenerator.cs
├── BlueBlaze.Bluesky/              # NuGetパッケージ2
│   ├── BlueskyClient.cs
│   └── Services/
└── lexicons/                        # Source Generator入力

tests/
├── BlueBlaze.AtProtocol.Tests/     # TUnit
├── BlueBlaze.Bluesky.Tests/        # TUnit
└── BlueBlaze.Specs/                # Reqnroll (日本語)

samples/
└── BlueBlaze.Quickstart/

.github/workflows/
├── ci.yml
├── aot-validation.yml
└── publish-nuget.yml
```

---

## Phase Outputs

**Phase 0 (Research)**: ✅ [research.md](research.md) - すべてのNEEDS CLARIFICATION解決済み

**Phase 1 (Design)**: ✅ 完了
- [data-model.md](data-model.md) - エンティティ、関係、バリデーション
- [contracts/](contracts/) - OpenAPI 3.1定義
- [quickstart.md](quickstart.md) - 20行/30行コードサンプル

**Phase 2 (Tasks)**: 次のステップ → `/speckit.tasks`

---

## Success Metrics

| 基準 | 測定方法 | 目標 |
|------|---------|------|
| SC-001 | quickstart.md行数 | ≤20行 |
| SC-002 | quickstart.md行数 | ≤30行 |
| SC-003 | BlueBlaze.AtProtocol単独ビルド | 成功 |
| SC-005 | 不正型のコンパイル | エラー |
| SC-006 | BenchmarkDotNet | <2秒 |

---

## Next Command

```bash
/speckit.tasks
```

tasks.mdで実装タスクの詳細分解とBDDワークフローを定義します。
