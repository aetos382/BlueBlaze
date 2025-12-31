# Requirements Quality Checklist: Architecture & Layered Design

**Purpose**: 二層クライアントライブラリの要件品質を検証（アーキテクチャ分離、API仕様、再利用性）
**Created**: 2026-01-01
**Feature**: [spec.md](../spec.md)
**Focus**: 機能要件の包括的カバレッジ（ライトウェイト）
**Depth**: 高優先度項目のみ（15-25項目）
**Scope**: 機能要件のみ（非機能要件は除外）

---

## Requirement Completeness

- [ ] CHK001 - AtProtocol層とBluesky層の境界と責任分担が明確に定義されているか？ [Completeness, Spec §FR-001]
- [ ] CHK002 - 各層が提供すべきインターフェースの完全なリストが文書化されているか？ [Gap]
- [ ] CHK003 - AtProtocol層の3つの主要機能（XRPC、DID解決、Lexiconスキーマ処理）それぞれの要件が完全に定義されているか？ [Completeness, Spec §FR-002]
- [ ] CHK004 - Bluesky層の3つの主要機能（投稿、プロフィール、フォロー）それぞれの要件が完全に定義されているか？ [Completeness, Spec §FR-005]
- [ ] CHK005 - 層間の依存関係と呼び出し方向が明確に文書化されているか？ [Gap]

---

## Requirement Clarity

- [ ] CHK006 - 「独立したモジュール」の定義が具体的に記述されているか（パッケージ分離、名前空間、ビルド単位など）？ [Clarity, Spec §FR-001]
- [ ] CHK007 - 「単独で動作可能」の基準が測定可能な形で定義されているか（依存関係なしでビルド・実行可能など）？ [Clarity, Spec §FR-003]
- [ ] CHK008 - 「明確に定義されたインターフェース」の内容が具体的に記述されているか（メソッドシグネチャ、データ型など）？ [Ambiguity, Spec §FR-006]
- [ ] CHK009 - 「適切なエラー処理」の基準が定量的に定義されているか（例外型、エラーコード、リトライロジックなど）？ [Ambiguity, Spec §FR-008]
- [ ] CHK010 - Lexiconスキーマのバリデーション要件が具体的に記述されているか（検証ルール、エラー報告形式など）？ [Clarity, Spec §FR-009]

---

## Requirement Consistency

- [ ] CHK011 - AtProtocol層の「認証とセッション管理」要件（FR-010）とBluesky層の利用要件が矛盾なく定義されているか？ [Consistency, Spec §FR-010]
- [ ] CHK012 - エラーハンドリング要件（FR-008）が両層で一貫した方針で記述されているか？ [Consistency]
- [ ] CHK013 - 「独立してテスト可能」要件（FR-006）と各層の受け入れシナリオが整合しているか？ [Consistency, Spec User Stories]

---

## Scenario Coverage

- [ ] CHK014 - AtProtocol層のXRPC呼び出しに関する正常系・異常系シナリオが網羅されているか？ [Coverage, Spec User Story 1]
- [ ] CHK015 - DID解決の失敗シナリオ（無効なDID、到達不能サーバーなど）が定義されているか？ [Coverage, Gap]
- [ ] CHK016 - Bluesky層の投稿操作（作成・取得・削除）の各段階でのエラーシナリオが定義されているか？ [Coverage, Spec User Story 2]
- [ ] CHK017 - 新しいAtProtocolアプリケーション追加時の拡張シナリオが具体的に記述されているか？ [Coverage, Spec User Story 3]

---

## Acceptance Criteria Quality

- [ ] CHK018 - 「20行以下のコード」（SC-001）の測定基準が明確に定義されているか（コメント含む/除く、インポート文の扱いなど）？ [Measurability, Spec §SC-001]
- [ ] CHK019 - 「明確に分離」（SC-003）の検証方法が客観的に定義されているか（ビルド独立性、依存関係グラフなど）？ [Measurability, Spec §SC-003]
- [ ] CHK020 - 「型安全性が保証される」（SC-005）の検証基準が具体的に記述されているか（コンパイル時チェック、静的解析など）？ [Measurability, Spec §SC-005]

---

## Edge Case Coverage

- [ ] CHK021 - Lexiconスキーマのバージョン不一致シナリオの要件が定義されているか？ [Edge Case, Spec §Edge Cases]
- [ ] CHK022 - 複数AtProtocolアプリケーションの同時インスタンス化時の分離要件が明確に記述されているか？ [Edge Case, Spec §Edge Cases]

---

## Traceability & Assumptions

- [ ] CHK023 - すべての機能要件（FR-001～FR-010）に対応する受け入れシナリオが存在するか？ [Traceability]
- [ ] CHK024 - 「両層は同じプログラミング環境で動作する」という前提条件が妥当か、または制約として明示すべきか？ [Assumption, Spec §Assumptions]
- [ ] CHK025 - Lexiconスキーマのバージョン管理方針が前提条件ではなく要件として定義されるべきか？ [Assumption, Spec §Assumptions]

---

## Completeness Verification

**Total Items**: 25
**Coverage**:
- アーキテクチャ分離: 5項目
- API仕様明確性: 5項目
- 一貫性: 3項目
- シナリオカバレッジ: 4項目
- 受け入れ基準: 3項目
- エッジケース: 2項目
- トレーサビリティ: 3項目

**Next Steps**: このチェックリストを使用して、spec.mdの要件品質を検証し、曖昧な要件や欠落している定義を特定してください。
