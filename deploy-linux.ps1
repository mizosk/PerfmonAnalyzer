# ======================================
# Linux (Ubuntu) 向けデプロイスクリプト
# ======================================
# このスクリプトは Linux サーバーで動かすためのビルドを行います
# ======================================

param(
    [string]$OutputPath = ".\publish-linux",
    [string]$TargetRuntime = "linux-x64",
    [switch]$SelfContained = $false,
    [string]$ServerAddress = "",
    [string]$ServerUser = "",
    [switch]$AutoTransfer
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Linux向けビルド開始" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "ターゲット: $TargetRuntime" -ForegroundColor Gray
Write-Host "Self-contained: $SelfContained" -ForegroundColor Gray
Write-Host ""

# プロジェクトのルートディレクトリを取得
$rootPath = $PSScriptRoot
$frontendPath = Join-Path $rootPath "src\frontend"
$backendPath = Join-Path $rootPath "src\backend\PerfmonAnalyzer.Api"
$distPath = Join-Path $frontendPath "dist"
$wwwrootPath = Join-Path $backendPath "wwwroot"

# ========================================
# ステップ1: フロントエンドのビルド
# ========================================
Write-Host "ステップ 1/3: フロントエンドをビルドしています..." -ForegroundColor Yellow
Write-Host "  場所: $frontendPath" -ForegroundColor Gray

Push-Location $frontendPath
try {
    Write-Host "  → 依存関係を確認中..." -ForegroundColor Gray
    npm install --silent
    
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

# ========================================
# ステップ2: 静的ファイルのコピー
# ========================================
Write-Host "ステップ 2/3: 静的ファイルをコピーしています..." -ForegroundColor Yellow
Write-Host "  コピー元: $distPath" -ForegroundColor Gray
Write-Host "  コピー先: $wwwrootPath" -ForegroundColor Gray

if (Test-Path $wwwrootPath) {
    Write-Host "  → 既存のファイルを削除中..." -ForegroundColor Gray
    Remove-Item $wwwrootPath -Recurse -Force
}

Write-Host "  → ファイルをコピー中..." -ForegroundColor Gray
Copy-Item $distPath $wwwrootPath -Recurse

Write-Host "  ✓ 静的ファイルのコピー完了" -ForegroundColor Green
Write-Host ""

# ========================================
# ステップ3: Linux用にビルド
# ========================================
Write-Host "ステップ 3/3: Linux向けにビルドしています..." -ForegroundColor Yellow
Write-Host "  場所: $backendPath" -ForegroundColor Gray
Write-Host "  出力先: $OutputPath" -ForegroundColor Gray

Push-Location $backendPath
try {
    $fullOutputPath = Join-Path $rootPath $OutputPath
    if (Test-Path $fullOutputPath) {
        Write-Host "  → 既存のビルド結果を削除中..." -ForegroundColor Gray
        Remove-Item $fullOutputPath -Recurse -Force
    }
    
    Write-Host "  → .NET アプリケーションをビルド中..." -ForegroundColor Gray
    Write-Host "    Runtime: $TargetRuntime" -ForegroundColor Gray
    Write-Host "    Self-contained: $SelfContained" -ForegroundColor Gray
    
    dotnet publish -c Release -r $TargetRuntime --self-contained:$SelfContained -o $fullOutputPath --nologo
    
    if (-not (Test-Path $fullOutputPath)) {
        throw "ビルドに失敗しました。出力フォルダが見つかりません。"
    }
    
    Write-Host "  ✓ Linux向けビルド完了" -ForegroundColor Green
}
finally {
    Pop-Location
}
Write-Host ""

# ========================================
# ステップ4: サーバーへ転送（オプション）
# ========================================
if ($AutoTransfer -and $ServerAddress -and $ServerUser) {
    Write-Host "ステップ 4/4: サーバーに転送しています..." -ForegroundColor Yellow
    Write-Host "  サーバー: $ServerUser@$ServerAddress" -ForegroundColor Gray
    
    $fullOutputPath = Join-Path $rootPath $OutputPath
    
    try {
        # SCP でファイル転送
        Write-Host "  → ファイルを転送中（パスワードを入力してください）..." -ForegroundColor Gray
        scp -r $fullOutputPath "${ServerUser}@${ServerAddress}:/tmp/perfmonanalyzer"
        
        Write-Host "  ✓ 転送完了" -ForegroundColor Green
        Write-Host ""
        Write-Host "次のステップ: サーバー側で以下を実行" -ForegroundColor Yellow
        Write-Host "  ssh $ServerUser@$ServerAddress" -ForegroundColor Gray
        Write-Host "  sudo systemctl stop perfmonanalyzer" -ForegroundColor Gray
        Write-Host "  sudo cp -r /tmp/perfmonanalyzer/* /opt/perfmonanalyzer/" -ForegroundColor Gray
        Write-Host "  sudo chmod +x /opt/perfmonanalyzer/PerfmonAnalyzer.Api" -ForegroundColor Gray
        Write-Host "  sudo systemctl start perfmonanalyzer" -ForegroundColor Gray
    }
    catch {
        Write-Host "  ✗ 転送に失敗しました: $_" -ForegroundColor Red
        Write-Host "  手動で転送してください" -ForegroundColor Yellow
    }
}

# ========================================
# 完了メッセージ
# ========================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Linux向けビルドが完了しました！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "出力フォルダ: $OutputPath" -ForegroundColor White
Write-Host ""

if (-not ($AutoTransfer -and $ServerAddress -and $ServerUser)) {
    Write-Host "次のステップ:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "【方法1】SCP で転送（推奨）" -ForegroundColor Cyan
    Write-Host "  Windowsから実行:" -ForegroundColor White
    Write-Host "    scp -r $OutputPath username@192.168.1.200:/tmp/perfmonanalyzer" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Ubuntuにログインして配置:" -ForegroundColor White
    Write-Host "    sudo systemctl stop perfmonanalyzer" -ForegroundColor Gray
    Write-Host "    sudo cp -r /tmp/perfmonanalyzer/* /opt/perfmonanalyzer/" -ForegroundColor Gray
    Write-Host "    sudo chmod +x /opt/perfmonanalyzer/PerfmonAnalyzer.Api" -ForegroundColor Gray
    Write-Host "    sudo chown -R perfmonapp:perfmonapp /opt/perfmonanalyzer" -ForegroundColor Gray
    Write-Host "    sudo systemctl start perfmonanalyzer" -ForegroundColor Gray
    Write-Host ""
    Write-Host "【方法2】自動転送" -ForegroundColor Cyan
    Write-Host "  次回から以下のコマンドで自動転送:" -ForegroundColor White
    Write-Host "    .\deploy-linux.ps1 -AutoTransfer -ServerAddress '192.168.1.200' -ServerUser 'username'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "【方法3】共有フォルダ（VirtualBox等）" -ForegroundColor Cyan
    Write-Host "  Ubuntuから:" -ForegroundColor White
    Write-Host "    sudo cp -r /media/sf_SharedFolder/$OutputPath/* /opt/perfmonanalyzer/" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "確認方法:" -ForegroundColor Yellow
Write-Host "  ブラウザで http://サーバーのIPアドレス:5000 にアクセス" -ForegroundColor White
Write-Host "  または: curl http://localhost:5000/api/health (サーバー上で実行)" -ForegroundColor White
Write-Host ""
