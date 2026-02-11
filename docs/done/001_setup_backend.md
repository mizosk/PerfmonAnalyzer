# タスク001: バックエンドプロジェクトのセットアップ

**ステータス**: 完了  
**優先度**: 高  
**見積もり**: 1時間

---

## 概要

ASP.NET Core Web API プロジェクトを作成し、基本的なプロジェクト構造を構築する。

## 前提条件

- .NET SDK 8.0 以上がインストールされていること
- Visual Studio Code または Visual Studio がインストールされていること

## 作業内容

### 1. ソリューション・プロジェクト作成

```powershell
cd c:\workspace\010_programs\perfmonAnalyzer\src
mkdir backend
cd backend
dotnet new sln -n PerfmonAnalyzer
dotnet new webapi -n PerfmonAnalyzer.Api
dotnet sln add PerfmonAnalyzer.Api
```

### 2. 必要なNuGetパッケージ追加

```powershell
cd PerfmonAnalyzer.Api
dotnet add package CsvHelper
dotnet add package Microsoft.AspNetCore.Cors
```

### 3. ディレクトリ構造作成

```
PerfmonAnalyzer.Api/
├── Controllers/
├── Services/
├── Models/
├── Program.cs
└── appsettings.json
```

### 4. CORS 設定

`Program.cs` に CORS ポリシーを追加（React からのアクセス許可）。

## 受け入れ基準

- [x] `dotnet build` が成功する
- [x] `dotnet run` で API が起動し、`http://localhost:5272/swagger` にアクセスできる
- [x] CORS が設定され、`http://localhost:5173` からアクセス可能

## 技術メモ

### dotnet CLI コマンド解説

| コマンド | 説明 |
|----------|------|
| `dotnet new sln` | ソリューションファイル（.sln）を作成 |
| `dotnet new webapi` | Web API プロジェクトのテンプレートを作成 |
| `dotnet sln add` | ソリューションにプロジェクトを追加 |
| `dotnet add package` | NuGet パッケージを追加 |

### CORS とは

**Cross-Origin Resource Sharing（CORS）** は、異なるオリジン（ドメイン）間での HTTP リクエストを許可する仕組みです。

- フロントエンド: `http://localhost:5173`（Vite 開発サーバー）
- バックエンド: `https://localhost:5001`（ASP.NET Core）

オリジンが異なるため、CORS 設定が必要です。

---

## 完了日

2026年2月1日
