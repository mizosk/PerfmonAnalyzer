# PerfmonAnalyzer

Windows パフォーマンスモニタ（Perfmon）で取得した CSV データを解析し、メモリリークやオブジェクトリークを検出・可視化する Web アプリケーションです。

## 主な機能

- **CSV ファイル読み込み** — Perfmon エクスポートの CSV（UTF-8 / Shift-JIS）を自動判定して取り込み
- **時系列グラフ表示** — Chart.js による対話的なグラフ。カウンタの表示切替、ツールチップ表示に対応
- **範囲選択** — グラフ上のドラッグ操作、または日時入力で解析範囲を指定
- **傾き算出（線形回帰）** — 選択範囲の各カウンタに対して最小二乗法で傾き（KB/10min）を算出し、閾値超過を警告表示
- **グラフ画像エクスポート** — PNG 形式でグラフ画像をダウンロード
- **レポート生成** — HTML / Markdown 形式の解析レポートを出力

## 技術スタック

| レイヤ | 技術 |
|--------|------|
| バックエンド | ASP.NET Core Web API (.NET 10) / C# |
| フロントエンド | React 19 / TypeScript / Vite |
| グラフ描画 | Chart.js + react-chartjs-2 |
| CSV パーサ | CsvHelper |
| テスト (Backend) | xUnit |
| テスト (Frontend) | Vitest + Testing Library |

## ディレクトリ構成

```
perfmonAnalyzer/
├── src/
│   ├── backend/                    # ASP.NET Core Web API
│   │   ├── PerfmonAnalyzer.Api/    # API プロジェクト
│   │   └── PerfmonAnalyzer.Api.Tests/ # バックエンドテスト
│   └── frontend/                   # React SPA
│       └── src/
│           ├── components/         # UI コンポーネント
│           ├── hooks/              # カスタム hooks
│           ├── plugins/            # Chart.js プラグイン
│           ├── services/           # API クライアント
│           └── types/              # 型定義
├── tests/                          # E2E テスト・テストデータ
├── scripts/                        # デプロイスクリプト (Linux)
├── docs/
│   ├── spec/                       # 要求仕様書
│   ├── design/                     # アーキテクチャ設計書
│   └── memo/                       # 技術メモ
├── deploy.ps1                      # Windows デプロイスクリプト
├── deploy-linux.ps1                # Linux デプロイスクリプト
└── DEPLOYMENT.md                   # デプロイガイド
```

## 前提条件

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v20 以降推奨)
- npm

## 開発環境のセットアップ

### バックエンドの起動

```bash
cd src/backend/PerfmonAnalyzer.Api
dotnet run
```

API は `https://localhost:7109`（または `http://localhost:5109`）で起動します。  
Swagger UI: `https://localhost:7109/swagger`

### フロントエンドの起動

```bash
cd src/frontend
npm install
npm run dev
```

開発サーバーは `http://localhost:5173` で起動します。  
バックエンド API へのリクエストは Vite のプロキシ、または CORS 設定により中継されます。

## テスト

### バックエンドテスト

```bash
cd src/backend
dotnet test
```

### フロントエンドテスト

```bash
cd src/frontend
npm test
```

## ビルド・デプロイ

### Windows

```powershell
# プロジェクトルートで実行
.\deploy.ps1
```

フロントエンドのビルド、静的ファイルのコピー、バックエンドの発行が自動で実行され、`publish/` フォルダに成果物が出力されます。

```powershell
# サーバーで実行
cd publish
dotnet PerfmonAnalyzer.Api.dll
```

### Linux

```powershell
# Windows 上でビルド
.\deploy-linux.ps1
```

詳細な手順は [DEPLOYMENT.md](DEPLOYMENT.md) および [scripts/LINUX_DEPLOY.md](scripts/LINUX_DEPLOY.md) を参照してください。

## API エンドポイント

| メソッド | パス | 説明 |
|----------|------|------|
| `POST` | `/api/file/upload` | CSV ファイルアップロード |
| `GET` | `/api/data/{sessionId}` | セッションデータ取得 |
| `POST` | `/api/analysis/slope` | 傾き（線形回帰）算出 |
| `POST` | `/api/report/generate` | レポート生成 |
| `GET` | `/api/health` | ヘルスチェック |

## ドキュメント

- [要求仕様書](docs/spec/requirements.md)
- [アーキテクチャ設計書](docs/design/architecture.md)
- [デプロイガイド](DEPLOYMENT.md)
- [Linux デプロイガイド](scripts/LINUX_DEPLOY.md)

## ライセンス

このプロジェクトは個人利用を目的としています。
