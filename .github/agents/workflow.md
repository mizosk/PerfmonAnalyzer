# エージェントオーケストレーション設計書

> perfmonエージェントシステムにおけるAIエージェント連携ワークフローの全体設計

## 1. アーキテクチャ概要

6つのエージェントが **階層型委譲モデル** で協調動作する。GitHub Issueを唯一の共有状態として、各エージェントが自律的に担当領域の作業を遂行する。

### エージェント階層

```
ユーザー / copilot-instructions.md
    │
    ▼
┌─────────┐
│  📋 PM  │ ← オーケストレータ（唯一のユーザー窓口）
└────┬────┘
     │ runSubAgent
     ├──────────────┬──────────────┐
     ▼              ▼              ▼
┌──────────┐ ┌──────────┐ ┌──────────┐
│🏗️Architect│ │💻 Coder  │ │🔍Reviewer│
└────┬─────┘ └────┬─────┘ └────┬─────┘
     │ runSubAgent │            │
     ├─────┬───┐   ├─────┬───┐  │
     ▼     ▼   │   ▼     ▼   │  ▼
 🔬Res  🔎Exp  │ 🔬Res 🔎Exp │ 🔎Exp
     │          │             │
     ▼          │             │
  🔎Exp         │             │
```

| 層 | エージェント | 性質 |
|----|------------|------|
| 指揮層 | **PM** | ユーザー要求をIssue化し、適切なエージェントに委譲。ブランチ・PR管理 |
| 専門層 | **Architect**, **Coder**, **Reviewer** | 各専門領域で自律的に作業を遂行 |
| 基盤層 | **Researcher**, **Explorer** | コンテキスト収集・分析のユーティリティ |

## 2. メインワークフロー

### 2.1 全体フロー（新機能開発）

```mermaid
sequenceDiagram
    actor User
    participant PM as 📋PM<br/>Issue・ブランチ管理
    participant Arch as 🏗️Architect<br/>設計
    participant Code as 💻Coder<br/>実装
    participant Rev as 🔍Reviewer<br/>品質保証
    participant GH as GitHub<br/>Issue / Branch / PR

    rect rgb(240, 248, 255)
        Note over User, GH: Phase 1: 準備
        User->>PM: 作業依頼
        PM->>GH: Issue作成（ラベル自動判定 + @meアサイン）
        PM->>GH: mainからブランチ作成（issue/<N>-<slug>）
    end

    rect rgb(240, 255, 240)
        Note over PM, GH: Phase 2: 設計
        PM->>Arch: 設計依頼 + Issue番号
        activate Arch
        Arch->>Arch: Researcher/Explorerに調査委譲
        Arch->>GH: Issue本文に設計・シーケンス図・実装計画を記載
        Arch-->>PM: 設計完了報告
        deactivate Arch
    end

    rect rgb(255, 255, 240)
        Note over PM, GH: Phase 3: 実装
        PM->>Code: 実装依頼 + Issue番号
        activate Code
        Code->>GH: Issue確認（設計内容）
        Code->>Code: Researcher/Explorerに調査委譲
        Code->>Code: 実装 + テスト + /docs更新
        Code->>GH: Issueチェックリスト更新
        Code-->>PM: 実装完了報告
        deactivate Code
    end

    rect rgb(255, 240, 240)
        Note over PM, GH: Phase 4: レビュー
        PM->>Rev: レビュー依頼 + Issue番号
        activate Rev
        Rev->>Rev: Explorerに整合性チェック委譲
        Rev->>GH: Issueコメントにレビュー結果投稿
        Rev-->>PM: レビューレポート（承認/差し戻し）
        deactivate Rev
    end

    rect rgb(248, 240, 255)
        Note over PM, GH: Phase 5: 完了
        alt 差し戻し
            PM->>Code: 修正指示
            Code-->>PM: 修正完了
            PM->>Rev: 再レビュー
            Rev-->>PM: 承認
        end
        PM->>GH: PR作成（Closes #<N>）
        PM->>GH: Issueクローズ
        PM-->>User: 完了報告
    end
```

### 2.2 バグ修正フロー（設計スキップ）

```mermaid
sequenceDiagram
    actor User
    participant PM as 📋PM
    participant Code as 💻Coder
    participant Rev as 🔍Reviewer
    participant GH as GitHub

    User->>PM: バグ報告
    PM->>GH: Issue作成（label: bug）+ ブランチ作成
    PM->>Code: 修正依頼
    Code->>Code: 修正 + テスト
    Code->>GH: Issueチェックリスト更新
    Code-->>PM: 完了
    PM->>Rev: レビュー依頼
    Rev->>GH: Issueコメントにレビュー結果
    Rev-->>PM: 承認
    PM->>GH: PR作成 + Issueクローズ
    PM-->>User: 完了報告
```

### 2.3 サブエージェント委譲フロー

```mermaid
sequenceDiagram
    participant Parent as 親エージェント<br/>(Architect/Coder/Reviewer)
    participant Res as 🔬Researcher
    participant Exp as 🔎Explorer

    rect rgb(245, 245, 255)
        Note over Parent, Exp: 並列調査（独立タスク時）
        par 技術調査
            Parent->>Res: 技術テーマの深掘り調査
            Res->>Exp: 大量ファイル探索
            Exp-->>Res: 構造化結果
            Res-->>Parent: 分析レポート
        and コードベース探索
            Parent->>Exp: 影響範囲特定
            Exp-->>Parent: ファイル一覧 + シンボル
        end
    end
```

## 3. エージェント詳細

### 3.1 📋 PM（プロジェクトマネージャー）

| 項目 | 内容 |
|------|------|
| **責務** | Issue作成、ラベル自動判定、アサイン、ブランチ作成、エージェント割当、PR作成、Issueクローズ、完了報告 |
| **委譲先** | Architect, Coder, Reviewer |
| **成果物** | GitHub Issue、ブランチ、PR、完了報告 |
| **判断基準** | 新機能→Architect経由、バグ修正→Coder直接 |

### 3.2 🏗️ Architect（設計者）

| 項目 | 内容 |
|------|------|
| **責務** | システム設計、アーキテクチャ決定、技術的意思決定 |
| **委譲先** | Researcher, Explorer |
| **成果物** | Issue本文への設計記載、`/docs` 設計書更新 |
| **制約** | コード編集・コマンド実行禁止（`gh issue` 操作は許可） |

### 3.3 💻 Coder（実装者）

| 項目 | 内容 |
|------|------|
| **責務** | コード実装、テスト作成・実行、設計書同期 |
| **委譲先** | Researcher, Explorer |
| **成果物** | 動作するコード、テスト結果、Issue進捗更新、`/docs` 更新 |
| **制約** | アーキテクチャ判断禁止、指示範囲外の実装禁止 |

### 3.4 🔍 Reviewer（品質保証）

| 項目 | 内容 |
|------|------|
| **責務** | コードレビュー、設計整合性検証、設計書同期 |
| **委譲先** | Explorer |
| **成果物** | Issueコメントへのレビュー結果投稿、`/docs` 更新 |
| **制約** | コード編集禁止（設計書編集は許可） |

### 3.5 🔬 Researcher（調査員）

| 項目 | 内容 |
|------|------|
| **責務** | 技術調査、設計オプション評価、シーケンス図作成、テストシナリオ設計 |
| **委譲先** | Explorer |
| **成果物** | 構造化された調査レポート |
| **制約** | コード編集・コマンド実行禁止 |

### 3.6 🔎 Explorer（探索員）

| 項目 | 内容 |
|------|------|
| **責務** | コードベース高速探索、情報収集、影響範囲特定 |
| **委譲先** | なし（リーフノード） |
| **成果物** | ファイル一覧、シンボル情報、構造化結果 |
| **制約** | 編集禁止、ユーザーへの質問禁止 |

## 4. 共有状態と引き継ぎ

### GitHub Issue が唯一の真実の源

```mermaid
graph LR
    PM -->|作成| Issue[GitHub Issue<br/>#N]
    Arch -->|設計記載| Issue
    Code -->|進捗更新| Issue
    Rev -->|コメント| Issue
    PM -->|PR作成| PR[Pull Request<br/>Closes #N]
    PR -->|マージ| Main[main]

    style Issue fill:#ffd,stroke:#333,stroke-width:2px
    style PR fill:#dfd,stroke:#333
    style Main fill:#ddf,stroke:#333
```

### ブランチ管理フロー

```mermaid
gitGraph
    commit id: "main"
    branch issue/42-add-feature
    checkout issue/42-add-feature
    commit id: "実装"
    commit id: "テスト"
    commit id: "レビュー修正"
    checkout main
    merge issue/42-add-feature id: "PR #43 (Closes #42)"
```

### 引き継ぎプロトコル

| 遷移 | 引き継ぎ内容 |
|------|------------|
| **PM → Architect** | Issue番号、タスク概要、関連設計（`/docs`）、制約事項 |
| **Architect → Coder** | Issue番号（設計記載済み）、変更対象ファイル、テスト観点 |
| **Coder → Reviewer** | Issue番号（実装済み）、レビュー観点 |
| **Reviewer → Coder**（差し戻し時） | 修正必須の指摘、修正方針、再レビュー確認ポイント |
| **Researcher → 親** | 設計オプション+トレードオフ、推奨案、影響ファイル |
| **Explorer → 親** | ファイル一覧+シンボル、発見内容、次のアクション |

## 5. 設計原則

### 5.1 コンテキスト効率化

- **委譲の判断基準**: 1000トークン超のコンテキストが必要なら委譲を検討
- **並列実行**: 独立タスクは `multi_tool_use.parallel` で同時委譲
- **Explorer の活用**: 大量ファイル探索は必ずExplorerに委譲し、親のコンテキストを温存

### 5.2 責務分離の徹底

- **設計する者は実装しない**: Architectはコードを書かない
- **実装する者は設計しない**: Coderはアーキテクチャ判断をしない
- **レビューする者はコードを直さない**: Reviewerはドキュメントのみ編集可
- **調査する者は判断しない**: Researcher/Explorerは情報提供のみ

### 5.3 品質ゲート

```mermaid
graph LR
    Impl[実装完了] --> Gate{レビュー}
    Gate -->|✅ 承認| PR[PR作成] --> Done[Issueクローズ]
    Gate -->|⚠️ 条件付き| Fix[軽微修正] --> Gate
    Gate -->|❌ 差し戻し| Rework[再実装] --> Gate
```

すべてのコード変更は **必ずReviewerを通過** する。差し戻しループは品質が満たされるまで継続する。

## 6. ルーティング判断フロー

```mermaid
flowchart TD
    Start[ユーザーからの依頼] --> Q1{設計変更が必要?}
    Q1 -->|Yes| Arch[Architect → Coder → Reviewer]
    Q1 -->|No| Q2{コード変更が必要?}
    Q2 -->|Yes| Code[Coder → Reviewer]
    Q2 -->|No| Q3{ドキュメントのみ?}
    Q3 -->|Yes| Direct[該当エージェントに直接依頼]
    Q3 -->|No| Direct

    style Start fill:#e8f4fd
    style Arch fill:#ffe8cc
    style Code fill:#e8ffe8
    style Direct fill:#f5f5f5
```

## 7. ラベル自動判定

| ラベル | 判定条件 |
|---|---|
| `bug` | バグ修正・不具合対応 |
| `enhancement` | 新機能・改善 |
| `refactor` | リファクタリング |
| `documentation` | ドキュメントのみの変更 |
| `design` | 設計フェーズが必要な作業 |
