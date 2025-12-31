# Feature Specification: AtProtocol・Bluesky 二層クライアント ライブラリ

**Feature Branch**: `001-atproto-bluesky-layers`
**Created**: 2026-01-01
**Status**: Draft
**Input**: User description: "このリポジトリはAtProtocolとBlueskyに関する広範なコードを含む。まずは既存のBlueskyサーバーに接続して操作を行う再利用可能なクライアント ライブラリを作成する。BlueskyはAtProtocol上のアプリケーションなので、クライアント ライブラリも二層構造とし、低レベルなAtProtocolとCore Lexiconの操作だけの機能を持つものと、BlueskyのLexiconを実装したものに分割する。"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - AtProtocol低レベル操作の実行 (Priority: P1)

開発者として、AtProtocolの基本的な操作（XRPC呼び出し、DID解決、Lexiconスキーマ処理）を実行したい。これにより、任意のAtProtocolアプリケーションと通信できる汎用基盤を構築できる。

**Why this priority**: AtProtocol層は、すべての上位アプリケーション（Bluesky含む）の基盤となる。この層がなければ、どのアプリケーション固有機能も実装できない。独立してテスト可能で、AtProtocol準拠の任意のサーバーと通信できる。

**Independent Test**: AtProtocolクライアントをインスタンス化し、XRPCリクエストを送信して応答を取得できることを確認。DID解決が正常に動作し、Lexiconスキーマのバリデーションが機能することを検証。

**Acceptance Scenarios**:

1. **Given** 有効なAtProtocolサーバーURL、**When** 開発者がXRPCクエリを実行する、**Then** 正しいレスポンスが返される
2. **Given** DID識別子、**When** 開発者がDID解決を実行する、**Then** DIDドキュメントが取得される
3. **Given** Lexiconスキーマ定義、**When** 開発者がデータをバリデーションする、**Then** スキーマ準拠性が検証される
4. **Given** ネットワークエラー、**When** XRPCリクエストが失敗する、**Then** 明確なエラーメッセージとリトライ可能性が通知される

---

### User Story 2 - Bluesky固有操作の実行 (Priority: P2)

開発者として、Bluesky特有の操作（投稿作成、プロフィール管理、フォロー関係）をBluesky LexiconのAPIを通じて実行したい。これにより、Blueskyアプリケーションの機能を迅速に実装できる。

**Why this priority**: P1のAtProtocol層の上に構築される。Bluesky Lexicon実装により、開発者はBluesky固有のビジネスロジックに集中でき、低レベル詳細を気にする必要がない。独立してテスト可能（AtProtocol層のモックを使用）。

**Independent Test**: Blueskyクライアントをインスタンス化し、app.bsky.feed.post Lexiconを使用して投稿を作成・取得・削除できることを確認。AtProtocol層が正常に動作していれば、Bluesky層も独立して機能する。

**Acceptance Scenarios**:

1. **Given** 認証済みBlueskyクライアント、**When** 開発者が投稿を作成する、**Then** app.bsky.feed.post Lexiconに従って投稿が作成される
2. **Given** Blueskyクライアント、**When** 開発者がプロフィールを取得する、**Then** app.bsky.actor.profile Lexiconに従ってプロフィールが返される
3. **Given** Blueskyクライアント、**When** 開発者がフォロー操作を実行する、**Then** app.bsky.graph.follow Lexiconに従ってフォロー関係が作成される

---

### User Story 3 - 他のAtProtocolアプリケーションへの拡張 (Priority: P3)

開発者として、AtProtocol層を再利用して、Bluesky以外のAtProtocolアプリケーションのクライアントを構築したい。これにより、エコシステム全体で一貫したクライアント開発体験を提供できる。

**Why this priority**: P1とP2が完了すれば、アーキテクチャの有効性を検証できる。将来のAtProtocolアプリケーション（例：WhiteWind、FrontPage）でも同じパターンが使える。

**Independent Test**: 新しいアプリケーション固有のLexiconを定義し、AtProtocol層のみを使用してそのアプリケーションと通信できることを確認。Bluesky層に依存せず動作する。

**Acceptance Scenarios**:

1. **Given** AtProtocol層のみを使用、**When** 開発者がカスタムLexiconを定義する、**Then** そのLexiconに従ってアプリケーションと通信できる
2. **Given** 新しいAtProtocolアプリケーション、**When** 開発者がAtProtocol層を再利用する、**Then** 最小限のコードで新アプリケーションクライアントを構築できる

---

### Edge Cases

- **ネットワーク接続が途中で切断された場合**: 両層で適切なタイムアウトとリトライロジックを提供
- **Lexiconスキーマが更新された場合**: バージョン管理とスキーマ検証により、互換性のない変更を検出
- **複数のAtProtocolアプリケーションを同時に使用する場合**: 各アプリケーション層は独立してインスタンス化可能
- **AtProtocolサーバーが標準外の実装を持つ場合**: AtProtocol層が厳格にプロトコル準拠を検証し、エラーを報告
- **Bluesky Lexiconが拡張された場合**: Bluesky層は拡張に対応しつつ、後方互換性を維持

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: ライブラリはAtProtocol層とBluesky層の2つの独立したモジュールで構成されなければならない
- **FR-002**: AtProtocol層は、XRPC呼び出し、DID解決、Lexiconスキーマ処理の機能を提供しなければならない
- **FR-003**: AtProtocol層は、Bluesky層に依存せず、単独で動作可能でなければならない
- **FR-004**: Bluesky層は、AtProtocol層の上に構築され、app.bsky.* Lexiconの操作を提供しなければならない
- **FR-005**: Bluesky層は、投稿作成・取得・削除、プロフィール管理、フォロー機能をサポートしなければならない
- **FR-006**: 両層は、明確に定義されたインターフェースで分離され、独立してテスト可能でなければならない
- **FR-007**: AtProtocol層は、将来の他のAtProtocolアプリケーション（Bluesky以外）でも再利用可能でなければならない
- **FR-008**: 各層は、適切なエラー処理とロギングを提供しなければならない
- **FR-009**: Lexiconスキーマのバリデーションは、AtProtocol層で実装されなければならない
- **FR-010**: 認証とセッション管理は、AtProtocol層で実装され、Bluesky層から利用されなければならない

### Key Entities

- **AtProtocolClient**: 低レベルAtProtocol操作を提供。XRPC呼び出し、DID解決、Lexiconバリデーション、セッション管理を含む
- **BlueskyClient**: Bluesky固有の操作を提供。AtProtocolClientを内部で使用。投稿、プロフィール、フォロー機能を含む
- **XRPCRequest/Response**: AtProtocolのXRPCメッセージを表現
- **DIDDocument**: DID解決の結果を表現
- **LexiconSchema**: Lexiconスキーマ定義とバリデーションロジック
- **Post/Profile/Follow**: Bluesky Lexiconのエンティティ（Bluesky層のみ）

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 開発者が20行以下のコードでAtProtocol層を使用して任意のXRPCリクエストを送信できる
- **SC-002**: 開発者が30行以下のコードでBluesky層を使用して投稿を作成・取得・削除できる
- **SC-003**: AtProtocol層とBluesky層のコードベースが明確に分離され、互いに独立してビルド・テスト可能である
- **SC-004**: 開発者が新しいAtProtocolアプリケーションのクライアントを、AtProtocol層を再利用して50行以下で実装できる
- **SC-005**: すべてのLexicon操作が、スキーマバリデーションを通過し、型安全性が保証される
- **SC-006**: 両層のAPI操作が通常の条件下で2秒以内に完了する
- **SC-007**: ライブラリが少なくとも2つの異なるAtProtocolアプリケーション（Blueskyを含む）で再利用される

## Assumptions

- AtProtocolサーバーは公式AtProtocol仕様に準拠している
- Blueskyサーバーはapp.bsky.* Lexiconの最新安定版を実装している
- 開発者は分離されたアーキテクチャの利点を理解している（関心の分離、再利用性）
- ネットワーク接続は比較的安定している
- Lexiconスキーマは適切にバージョン管理されている
- 両層は同じプログラミング環境で動作する
- パフォーマンス要件は標準的なWebアプリケーションの期待値に基づく（レスポンスタイム2秒以内）
