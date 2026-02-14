# ======================================
# Perfmon Analyzer デプロイスクリプト
# ======================================
# このスクリプトは以下の処理を自動化します：
# 1. フロントエンドのビルド（React → 静的ファイル）
# 2. ビルド結果をバックエンドにコピー
# 3. バックエンドを本番用に発行（Publish）
# ======================================

param(
    [string]$OutputPath = ".\publish",
    [switch]$SkipFrontend,
    [switch]$SkipBackend
)

# エラー時に停止
$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Perfmon Analyzer デプロイ開始" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# プロジェクトのルートディレクトリを取得
# $PSScriptRoot = このスクリプトがあるフォルダ（プロジェクトルート）
$rootPath = $PSScriptRoot
$frontendPath = Join-Path $rootPath "src\frontend"
$backendPath = Join-Path $rootPath "src\backend\PerfmonAnalyzer.Api"
$distPath = Join-Path $frontendPath "dist"
$wwwrootPath = Join-Path $backendPath "wwwroot"

# ========================================
# ステップ1: フロントエンドのビルド
# ========================================
if (-not $SkipFrontend) {
    Write-Host "ステップ 1/3: フロントエンドをビルドしています..." -ForegroundColor Yellow
    Write-Host "  場所: $frontendPath" -ForegroundColor Gray
    
    Push-Location $frontendPath
    try {
        # npm install で依存関係を確認
        Write-Host "  → 依存関係を確認中..." -ForegroundColor Gray
        npm install --silent
        
        # ビルド実行
        Write-Host "  → ビルドを実行中..." -ForegroundColor Gray
        npm run build
        
        if (-not (Test-Path $distPath)) {
            throw "ビルドに失敗しました。dist フォルダが見つかりません。"
        }
        
        Write-Host "  ✓ フロントエンドのビルド完了" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
    Write-Host ""
}
else {
    Write-Host "ステップ 1/3: フロントエンドのビルドをスキップ" -ForegroundColor Gray
    Write-Host ""
}

# ========================================
# ステップ2: 静的ファイルのコピー
# ========================================
if (-not $SkipFrontend) {
    Write-Host "ステップ 2/3: 静的ファイルをコピーしています..." -ForegroundColor Yellow
    Write-Host "  コピー元: $distPath" -ForegroundColor Gray
    Write-Host "  コピー先: $wwwrootPath" -ForegroundColor Gray
    
    # 既存の wwwroot を削除
    if (Test-Path $wwwrootPath) {
        Write-Host "  → 既存のファイルを削除中..." -ForegroundColor Gray
        Remove-Item $wwwrootPath -Recurse -Force
    }
    
    # dist フォルダを wwwroot にコピー
    Write-Host "  → ファイルをコピー中..." -ForegroundColor Gray
    Copy-Item $distPath $wwwrootPath -Recurse
    
    Write-Host "  ✓ 静的ファイルのコピー完了" -ForegroundColor Green
    Write-Host ""
}
else {
    Write-Host "ステップ 2/3: 静的ファイルのコピーをスキップ" -ForegroundColor Gray
    Write-Host ""
}

# ========================================
# ステップ3: バックエンドの発行
# ========================================
if (-not $SkipBackend) {
    Write-Host "ステップ 3/3: バックエンドを発行しています..." -ForegroundColor Yellow
    Write-Host "  場所: $backendPath" -ForegroundColor Gray
    Write-Host "  出力先: $OutputPath" -ForegroundColor Gray
    
    Push-Location $backendPath
    try {
        # 既存の publish フォルダを削除
        $fullOutputPath = Join-Path $rootPath $OutputPath
        if (Test-Path $fullOutputPath) {
            Write-Host "  → 既存の発行ファイルを削除中..." -ForegroundColor Gray
            Remove-Item $fullOutputPath -Recurse -Force
        }
        
        # dotnet publish を実行
        Write-Host "  → .NET アプリケーションを発行中..." -ForegroundColor Gray
        dotnet publish -c Release -o $fullOutputPath --nologo
        
        if (-not (Test-Path $fullOutputPath)) {
            throw "発行に失敗しました。出力フォルダが見つかりません。"
        }
        
        Write-Host "  ✓ バックエンドの発行完了" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
    Write-Host ""
}
else {
    Write-Host "ステップ 3/3: バックエンドの発行をスキップ" -ForegroundColor Gray
    Write-Host ""
}

# ========================================
# 完了メッセージ
# ========================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " デプロイ準備が完了しました！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "次のステップ:" -ForegroundColor Yellow
Write-Host "  1. publish フォルダをサーバーPCにコピー" -ForegroundColor White
Write-Host "  2. サーバーで以下のコマンドを実行:" -ForegroundColor White
Write-Host "     cd $OutputPath" -ForegroundColor Gray
Write-Host "     dotnet PerfmonAnalyzer.Api.dll" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. ブラウザでアクセス:" -ForegroundColor White
Write-Host "     http://localhost:5000" -ForegroundColor Gray
Write-Host "     または" -ForegroundColor Gray
Write-Host "     http://<サーバーのIPアドレス>:5000" -ForegroundColor Gray
Write-Host ""
Write-Host "注意事項:" -ForegroundColor Yellow
Write-Host "  • 他のPCからアクセスする場合は、appsettings.json で" -ForegroundColor White
Write-Host "    Urls を 'http://0.0.0.0:5000' に変更してください" -ForegroundColor White
Write-Host "  • ファイアウォールで5000番ポートを開放してください" -ForegroundColor White
Write-Host ""
