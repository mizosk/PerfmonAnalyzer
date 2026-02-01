# タスク001: プロジェクトセットアップ（WinForms版）

**ステータス**: 完了  
**優先度**: 高  
**完了日**: 2026年2月1日

---

## 概要

C# .NET 8 WinForms プロジェクトを作成し、基本的なプロジェクト構造を構築した。

## 実施内容

### 1. ソリューション・プロジェクト作成

- `PerfmonAnalyzer.sln` - ソリューションファイル
- `src/PerfmonAnalyzer/` - WinForms メインプロジェクト
- `tests/PerfmonAnalyzer.Tests/` - xUnit テストプロジェクト

### 2. フォルダ構造（レイヤードアーキテクチャ対応）

```
PerfmonAnalyzer/
├── PerfmonAnalyzer.sln
├── .editorconfig
├── .gitignore
├── src/
│   └── PerfmonAnalyzer/
│       ├── Models/          # ドメインモデル
│       │   ├── DataPoint.cs
│       │   ├── CounterInfo.cs
│       │   └── SlopeResult.cs
│       ├── Services/        # ビジネスロジック
│       ├── Views/           # フォーム（UI）
│       ├── Form1.cs
│       ├── Program.cs
│       └── PerfmonAnalyzer.csproj
└── tests/
    └── PerfmonAnalyzer.Tests/
        ├── DataPointTests.cs
        └── PerfmonAnalyzer.Tests.csproj
```

### 3. 作成したドメインモデル

- `DataPoint` - 時系列データポイント（タイムスタンプと値）
- `CounterInfo` - カウンタ情報（名前、プロセス名、カウンタタイプ、データリスト）
- `SlopeResult` - 傾き算出結果（傾き、警告フラグ、決定係数）

### 4. EditorConfig

C# コーディング規約を定義：
- インデント: スペース 4文字
- 命名規則: PascalCase（パブリックメンバー）、_camelCase（プライベートフィールド）
- ファイルスコープ名前空間を使用
- var の使用推奨

### 5. .gitignore更新

テスト結果フォルダ（TestResults/）を追加

## 確認結果

- [x] `dotnet build` が成功する
- [x] `dotnet test` でテストが実行できる（3テスト全て成功）
- [x] EditorConfig によるコーディング規約が適用されている

## 技術メモ

### ターゲットフレームワークの互換性

WinForms プロジェクトは `net8.0-windows` をターゲットとするため、テストプロジェクトも同じターゲットフレームワークに設定する必要がある。

```xml
<TargetFramework>net8.0-windows</TargetFramework>
```
