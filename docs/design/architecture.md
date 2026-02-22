# アーキテクチャ設計書: PerfmonAnalyzer

**バージョン**: 1.0  
**作成日**: 2026年1月31日

---

## 1. 全体構成

```
┌─────────────────────────────────────────────────────────────────┐
│                        Web Browser                               │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              React Frontend (SPA)                        │    │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐   │    │
│  │  │ファイル   │ │グラフ    │ │傾き      │ │画像      │   │    │
│  │  │アップロード│ │表示     │ │サマリ    │ │エクスポート│   │    │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘   │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │ HTTP (REST API)
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   ASP.NET Core Web API                          │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                    Controllers                            │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │   │
│  │  │ FileController│  │DataController │  │AnalysisController│ │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                     Services                              │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │   │
│  │  │ CsvImporter  │  │ DataService  │  │SlopeAnalyzer │   │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                      Models                               │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │   │
│  │  │ TimeSeriesData│  │ CounterInfo  │  │ SlopeResult  │   │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. レイヤ構成

### 2.1 プレゼンテーション層（Frontend）

| コンポーネント | 責務 |
|----------------|------|
| **FileUpload** | CSV ファイルのアップロード UI |
| **ChartView** | Chart.js を使った時系列グラフ表示 |
| **RangeSelector** | 時間範囲の選択（入力フィールド or ドラッグ） |
| **SlopeSummary** | 傾き一覧テーブルと警告表示 |
| **ExportButton** | グラフ画像のダウンロード |

**技術選定理由**:
- **React**: コンポーネントベースで UI を分割しやすく、状態管理が明確
- **Chart.js（react-chartjs-2）**: React との統合が容易、`toBase64Image()` で画像出力可能

### 2.2 API 層（Controllers）

| コントローラ | エンドポイント | 責務 |
|--------------|----------------|------|
| **FileController** | `POST /api/file/upload` | CSV アップロード受付 |
| **DataController** | `GET /api/data/{sessionId}` | 読み込み済みデータ取得 |
| **AnalysisController** | `POST /api/analysis/slope` | 傾き算出リクエスト |

### 2.3 ビジネスロジック層（Services）

| サービス | 責務 |
|----------|------|
| **CsvImporter** | CSV パース、データモデル変換、エンコーディング自動判定 |
| **DataService** | セッション単位のデータ保持、フィルタリング |
| **SlopeAnalyzer** | 線形回帰（OLS）による傾き算出、閾値判定 |
| **ReportGenerator** | レポート生成のファサード。Strategy パターンにより出力形式ごとの生成ロジックを委譲 |
| **IReportFormatStrategy** | レポート出力形式の Strategy インターフェース |
| **HtmlReportStrategy** | HTML 形式のレポート生成 Strategy |
| **MarkdownReportStrategy** | Markdown 形式のレポート生成 Strategy |
| **ReportUtilities** | レポート生成で共通利用するユーティリティ（カウンタ名解析、Base64 検証等） |

### 2.4 ドメインモデル層（Models）

```csharp
// 時系列データポイント
public class DataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}

// カウンタ情報
public class CounterInfo
{
    public string Name { get; set; }        // 例: "Process(app)\Private Bytes"
    public string ProcessName { get; set; } // 例: "app"
    public string CounterType { get; set; } // 例: "Private Bytes"
    public List<DataPoint> Data { get; set; }
}

// 傾き算出結果
public class SlopeResult
{
    public string CounterName { get; set; }
    public double SlopeKBPer10Min { get; set; }  // 傾き（KB/10min）
    public bool IsWarning { get; set; }          // 閾値超過フラグ
    public double RSquared { get; set; }         // 決定係数（参考）
}
```

---

## 3. データフロー

### 3.1 CSV アップロード → グラフ表示

```
[ユーザー] 
    │ CSVファイル選択
    ▼
[FileUpload Component]
    │ POST /api/file/upload (multipart/form-data)
    ▼
[FileController]
    │ CsvImporter.Import(stream)
    ▼
[CsvImporter]
    │ 1. エンコーディング判定
    │ 2. ヘッダ解析（カウンタ名抽出）
    │ 3. データ行パース
    │ 4. CounterInfo リスト生成
    ▼
[DataService]
    │ セッションIDを発行し、データを保持
    ▼
[FileController]
    │ Response: { sessionId, counters: [...] }
    ▼
[ChartView Component]
    │ GET /api/data/{sessionId}
    │ グラフ描画
    ▼
[ユーザー] グラフ表示
```

### 3.2 範囲選択 → 傾き算出

```
[ユーザー]
    │ 時間範囲を選択（2026/01/15 10:00 〜 2026/01/15 20:00）
    ▼
[RangeSelector Component]
    │ POST /api/analysis/slope
    │ Body: { sessionId, startTime, endTime, thresholdKBPer10Min: 50 }
    ▼
[AnalysisController]
    │ SlopeAnalyzer.Calculate(data, range, threshold)
    ▼
[SlopeAnalyzer]
    │ 1. 指定範囲のデータ抽出
    │ 2. 各カウンタに対して線形回帰（OLS）
    │ 3. 傾きを KB/10min に変換
    │ 4. 閾値判定
    ▼
[AnalysisController]
    │ Response: { results: [{ counterName, slopeKBPer10Min, isWarning, rSquared }, ...] }
    ▼
[SlopeSummary Component]
    │ テーブル表示、警告色適用
    ▼
[ユーザー] 傾きサマリ確認
```

---

## 4. 傾き算出アルゴリズム

### 4.1 線形回帰（最小二乗法）

時系列データ $(t_i, y_i)$ に対して、直線 $y = a \cdot t + b$ をフィットさせる。

傾き $a$ の算出式：

$$
a = \frac{n \sum t_i y_i - \sum t_i \sum y_i}{n \sum t_i^2 - (\sum t_i)^2}
$$

### 4.2 単位変換

Perfmon の値は **バイト単位**、時間軸は **分単位** で取得される。

傾きを **KB/10min** に変換する式：

$$
\text{傾き}_{KB/10min} = a \times 10 \div 1024
$$

ここで：
- $a$: 生の傾き（bytes/min）
- $\times 10$: 1分あたり → 10分あたり
- $\div 1024$: バイト → KB

### 4.3 決定係数（R²）

フィット精度の指標として決定係数も算出（参考表示用）：

$$
R^2 = 1 - \frac{\sum (y_i - \hat{y}_i)^2}{\sum (y_i - \bar{y})^2}
$$

---

## 5. ディレクトリ構成

```
src/
├── backend/                          # ASP.NET Core プロジェクト
│   ├── PerfmonAnalyzer.Api/
│   │   ├── Controllers/
│   │   │   ├── FileController.cs
│   │   │   ├── DataController.cs
│   │   │   └── AnalysisController.cs
│   │   ├── Services/
│   │   │   ├── CsvImporter.cs
│   │   │   ├── DataService.cs
│   │   │   └── SlopeAnalyzer.cs
│   │   ├── Models/
│   │   │   ├── DataPoint.cs
│   │   │   ├── CounterInfo.cs
│   │   │   └── SlopeResult.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   └── PerfmonAnalyzer.Api.sln
│
└── frontend/                         # React プロジェクト
    ├── src/
    │   ├── components/
    │   │   ├── FileUpload.tsx
    │   │   ├── ChartView.tsx
    │   │   ├── RangeSelector.tsx
    │   │   ├── SlopeSummary.tsx
    │   │   └── ExportButton.tsx
    │   ├── services/
    │   │   └── api.ts
    │   ├── types/
    │   │   └── index.ts
    │   ├── App.tsx
    │   └── main.tsx
    ├── package.json
    └── vite.config.ts
```

---

## 6. API 仕様概要

### 6.1 ファイルアップロード

```
POST /api/file/upload
Content-Type: multipart/form-data

Response 200:
{
  "sessionId": "abc123",
  "counters": [
    { "name": "Process(app)\\Private Bytes", "processName": "app", "counterType": "Private Bytes", "dataCount": 3600 },
    ...
  ]
}
```

### 6.2 データ取得

```
GET /api/data/{sessionId}?startTime=2026-01-15T10:00:00&endTime=2026-01-15T20:00:00

Response 200:
{
  "counters": [
    {
      "name": "Process(app)\\Private Bytes",
      "data": [
        { "timestamp": "2026-01-15T10:00:00", "value": 1234567 },
        ...
      ]
    }
  ]
}
```

### 6.3 傾き算出

```
POST /api/analysis/slope
Content-Type: application/json
{
  "sessionId": "abc123",
  "startTime": "2026-01-15T10:00:00",
  "endTime": "2026-01-15T20:00:00",
  "thresholdKBPer10Min": 50
}

Response 200:
{
  "results": [
    { "counterName": "Process(app)\\Private Bytes", "slopeKBPer10Min": 75.3, "isWarning": true, "rSquared": 0.92 },
    { "counterName": "Process(app)\\Handle Count", "slopeKBPer10Min": 0.5, "isWarning": false, "rSquared": 0.45 }
  ]
}
```

---

## 7. 技術解説（補足）

### 7.1 ASP.NET Core とは

**ASP.NET Core** は Microsoft が開発した Web アプリケーションフレームワークです。

- **クロスプラットフォーム**: Windows, Linux, macOS で動作
- **高パフォーマンス**: 軽量で高速
- **依存性注入（DI）**: サービスの疎結合化が容易

基本的な流れ：
```
HTTP リクエスト → Controller → Service → Response
```

### 7.2 React とは

**React** は Facebook（Meta）が開発した UI ライブラリです。

- **コンポーネント**: UI を小さな部品に分割して再利用
- **状態管理**: `useState`, `useEffect` などのフックで状態を管理
- **仮想DOM**: 効率的な画面更新

### 7.3 Chart.js とは

**Chart.js** はオープンソースのグラフ描画ライブラリです。

- **多様なグラフ**: 折れ線、棒、円など
- **レスポンシブ**: 画面サイズに自動調整
- **画像出力**: `toBase64Image()` で PNG 取得可能

---

## 変更履歴

| バージョン | 日付 | 変更内容 |
|------------|------|----------|
| 1.0 | 2026/01/31 | 初版作成 |
| 1.1 | 2026/02/22 | ReportGenerator に Strategy パターン適用（Issue #6） |
