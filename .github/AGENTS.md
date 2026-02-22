# perfmon エージェントシステム

GitHub Issueを唯一の共有状態として、6つのエージェントが階層型委譲モデルで協調動作する。

## エージェント一覧

| エージェント | 絵文字 | 責務 | 委譲先 |
|---|---|---|---|
| `perfmon.pm` | 📋 | Issue作成・エージェント割当・ブランチ/PR管理・完了報告 | Architect, Coder, Reviewer |
| `perfmon.architect` | 🏗️ | 設計・アーキテクチャ決定・Issue本文に設計記載 | Researcher, Explorer |
| `perfmon.coder` | 💻 | 実装・テスト・Issue進捗更新 | Researcher, Explorer |
| `perfmon.reviewer` | 🔍 | コードレビュー・Issueコメントに結果投稿 | Explorer |
| `perfmon.researcher` | 🔬 | 技術調査・分析・構造化レポート返却 | Explorer |
| `perfmon.explorer` | 🔎 | コードベース高速探索・情報収集 | なし |

## ワークフロー

1. **準備**: PM が Issue 作成 + `main` からブランチ作成 (`issue/<N>-<slug>`)
2. **設計** (必要時): Architect が Issue 本文に設計記載 + `/docs` 更新
3. **実装**: Coder が設計に従い実装・テスト + Issue チェックリスト更新
4. **レビュー**: Reviewer が Issue コメントにレビュー結果投稿
5. **完了**: PM が PR 作成 (`Closes #<N>`) → Issue クローズ

詳細は `agents/workflow.md` を参照。

## ルーティング

- 設計変更あり → Architect → Coder → Reviewer
- バグ修正/小規模変更 → Coder → Reviewer
- ドキュメントのみ → 該当エージェントに直接

## 責務分離

- 設計する者はコードを書かない (Architect)
- 実装する者は設計判断をしない (Coder)
- レビューする者はコードを直さない (Reviewer: `/docs` 編集のみ許可)
- 調査する者は判断しない (Researcher / Explorer: 情報提供のみ)
