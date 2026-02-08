# バックエンドソースコード解説

**調査日**: 2026年2月1日

## 概要

`src/backend` ディレクトリには、ASP.NET Core Web API を使用したバックエンドプロジェクトが含まれています。パフォーマンスモニタ（Perfmon）のデータを解析するためのREST APIを提供します。

## プロジェクト構成

### プロジェクト一覧

1. **PerfmonAnalyzer.Api** - メインのWeb APIプロジェクト
2. **PerfmonAnalyzer.Api.Tests** - 統合テストプロジェクト

### 技術スタック

- **フレームワーク**: .NET 10.0
- **プロジェクトタイプ**: ASP.NET Core Web API
- **主要NuGetパッケージ**:
  - `CsvHelper` (v33.1.0) - CSVファイルの解析用
  - `Swashbuckle.AspNetCore` (v10.1.1) - Swagger/OpenAPIドキュメント生成
  - `Microsoft.AspNetCore.OpenApi` (v10.0.1) - OpenAPI仕様サポート

## プロジェクト詳細

### 1. Program.cs - アプリケーションエントリーポイント

[Program.cs](../src/backend/PerfmonAnalyzer.Api/Program.cs) は、ASP.NET Core の最小ホスティングモデルを使用しています。

#### 主要な設定

**サービス登録**:
```csharp
builder.Services.AddControllers();
```
- MVC コントローラーを有効化

**CORS設定**:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```
- React開発サーバー (`http://localhost:5173`) からのクロスオリジンリクエストを許可
- すべてのHTTPヘッダーとメソッドを許可

**Swagger/OpenAPI**:
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```
- API仕様を自動生成し、ブラウザで確認可能なSwagger UIを提供

**ミドルウェアパイプライン**:
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseCors("AllowReactDev");
app.UseAuthorization();
app.MapControllers();
```
- 開発環境でSwagger UIを有効化
- HTTPSへのリダイレクトを強制
- CORS ポリシーを適用
- 認可ミドルウェアを追加
- コントローラーのルートをマッピング

**テスト用の部分クラス**:
```csharp
public partial class Program { }
```
- `WebApplicationFactory<Program>` を使った統合テストを可能にする

### 2. HealthController.cs - ヘルスチェックエンドポイント

[HealthController.cs](../src/backend/PerfmonAnalyzer.Api/Controllers/HealthController.cs) は、APIの稼働状態を確認するためのシンプルなコントローラーです。

#### エンドポイント詳細

- **ルート**: `GET /api/health`
- **レスポンス**: 
  ```json
  {
    "status": "healthy",
    "timestamp": "2026-02-01T12:00:00.0000000Z"
  }
  ```
- **HTTPステータスコード**: 200 OK

#### 実装の特徴

- `[ApiController]` 属性により、モデル検証やエラーハンドリングが自動化
- `[Route("api/[controller]")]` により、コントローラー名 (`Health`) に基づいたルーティング
- UTC タイムスタンプを含むことで、サーバーの時刻を確認可能

### 3. launchSettings.json - 起動設定

[launchSettings.json](../src/backend/PerfmonAnalyzer.Api/Properties/launchSettings.json) には、開発環境での起動プロファイルが定義されています。

#### プロファイル

1. **http**:
   - URL: `http://localhost:5272`
   - ブラウザの自動起動: なし

2. **https**:
   - URL: `https://localhost:7086` (HTTPS), `http://localhost:5272` (HTTP)
   - ブラウザの自動起動: なし

両方のプロファイルで環境変数 `ASPNETCORE_ENVIRONMENT` が `Development` に設定されています。

### 4. テストプロジェクト (PerfmonAnalyzer.Api.Tests)

[HealthControllerTests.cs](../src/backend/PerfmonAnalyzer.Api.Tests/HealthControllerTests.cs) では、統合テストが実装されています。

#### テストの内容

1. **GetHealth_ReturnsOk**:
   - `/api/health` へのGETリクエストが 200 OK を返すことを検証

2. **GetHealth_ReturnsHealthyStatus**:
   - レスポンスのJSONが正しい形式 (`status` フィールドが `"healthy"`) であることを検証

#### テスト技術

- `WebApplicationFactory<Program>` を使用したインメモリテスト
- `IClassFixture` パターンによるテストフィクスチャの共有
- xUnit テストフレームワークを使用

## ブラウザでレスポンスを受け取る方法

### 方法1: Swagger UIを使用（推奨）

1. **APIを起動**:
   ```bash
   cd c:\workspace\010_programs\perfmonAnalyzer\src\backend\PerfmonAnalyzer.Api
   dotnet run
   ```

2. **Swagger UIにアクセス**:
   - HTTP: `http://localhost:5272/swagger`
   - HTTPS: `https://localhost:7086/swagger`

3. **エンドポイントをテスト**:
   - Swagger UIで `/api/Health` の `GET` をクリック
   - "Try it out" ボタンをクリック
   - "Execute" ボタンをクリック
   - レスポンスが表示される

### 方法2: ブラウザで直接アクセス

1. **APIを起動**（上記と同じ）

2. **URLに直接アクセス**:
   - HTTP: `http://localhost:5272/api/health`
   - HTTPS: `https://localhost:7086/api/health`

3. **レスポンス表示**:
   ```json
   {"status":"healthy","timestamp":"2026-02-01T12:00:00.0000000Z"}
   ```

### 方法3: ブラウザの開発者ツール（Console）

```javascript
fetch('http://localhost:5272/api/health')
  .then(response => response.json())
  .then(data => console.log(data));
```

### 方法4: Reactアプリケーションから

CORS設定により、React開発サーバー (`http://localhost:5173`) からのアクセスが許可されています。

```javascript
// Reactコンポーネント内
useEffect(() => {
  fetch('http://localhost:5272/api/health')
    .then(response => response.json())
    .then(data => console.log(data))
    .catch(error => console.error(error));
}, []);
```

## ディレクトリ構造の意図

```
PerfmonAnalyzer.Api/
├── Controllers/        # APIエンドポイントの実装
├── Models/            # データモデル（現在は空）
├── Services/          # ビジネスロジック（現在は空）
├── Properties/        # 起動設定
├── Program.cs         # アプリケーションのエントリーポイント
└── appsettings.json   # アプリケーション設定
```

この構造は、ASP.NET Core のベストプラクティスに従っており：
- **Controllers**: HTTPリクエストの受付とレスポンス
- **Models**: データ構造の定義
- **Services**: ビジネスロジックの実装（Controllerから分離）

将来的に、CSV解析やデータ分析のロジックは `Services/` ディレクトリに、データモデルは `Models/` ディレクトリに追加される予定です。

## 現在の実装状態

現時点では、以下が完成しています：
- ✅ プロジェクトの基本セットアップ
- ✅ CORS設定（Reactフロントエンドとの連携準備）
- ✅ Swagger/OpenAPI統合
- ✅ ヘルスチェックエンドポイント
- ✅ 統合テストの基盤

今後の実装予定（`docs/todo` を参照）：
- CSV インポート機能
- 傾き分析ロジック
- グラフデータ提供API
- 傾きサマリーAPI
