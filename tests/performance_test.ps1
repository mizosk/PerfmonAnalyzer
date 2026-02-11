# パフォーマンステストスクリプト

Write-Host "=== パフォーマンステスト ===" -ForegroundColor Cyan
Write-Host ""

# 1. CSV アップロード（60時間分）
Write-Host "[1/3] CSV アップロード（3600ポイント）..." -ForegroundColor Yellow
$uploadTime = Measure-Command {
    $result = curl -X POST http://localhost:5272/api/file/upload `
        -F "file=@C:\workspace\010_programs\perfmonAnalyzer\tests\test_data_60h.csv" `
        2>&1 | Out-Null
}
Write-Host "  結果: $([math]::Round($uploadTime.TotalSeconds, 2)) 秒" -ForegroundColor $(if ($uploadTime.TotalSeconds -le 10) { 'Green' } else { 'Red' })
Write-Host "  目標: 10秒以内" -ForegroundColor Gray
Write-Host ""

# 2. データ取得
Write-Host "[2/3] データ取得..." -ForegroundColor Yellow
$sessionId = (Get-Content C:\workspace\010_programs\perfmonAnalyzer\tests\upload_result.json | ConvertFrom-Json).sessionId
$dataTime = Measure-Command {
    $data = curl -X GET "http://localhost:5272/api/data/$sessionId" 2>&1 | Out-Null
}
Write-Host "  結果: $([math]::Round($dataTime.TotalMilliseconds, 0)) ミリ秒" -ForegroundColor $(if ($dataTime.TotalMilliseconds -le 1000) { 'Green' } else { 'Red' })
Write-Host "  目標: 1秒以内（グラフ描画）" -ForegroundColor Gray
Write-Host ""

# 3. 傾き算出
Write-Host "[3/3] 傾き算出..." -ForegroundColor Yellow
$analysisTime = Measure-Command {
    $analysis = curl -X POST http://localhost:5272/api/analysis/slope `
        -H "Content-Type: application/json" `
        --data "@C:\workspace\010_programs\perfmonAnalyzer\tests\slope_request.json" `
        2>&1 | Out-Null
}
Write-Host "  結果: $([math]::Round($analysisTime.TotalMilliseconds, 0)) ミリ秒" -ForegroundColor $(if ($analysisTime.TotalMilliseconds -le 500) { 'Green' } else { 'Red' })
Write-Host "  目標: 500ミリ秒以内" -ForegroundColor Gray
Write-Host ""

# サマリ
Write-Host "=== サマリ ===" -ForegroundColor Cyan
$allPassed = $uploadTime.TotalSeconds -le 10 -and $dataTime.TotalMilliseconds -le 1000 -and $analysisTime.TotalMilliseconds -le 500

if ($allPassed) {
    Write-Host "✓ すべてのパフォーマンス目標を達成しました！" -ForegroundColor Green
} else {
    Write-Host "✗ 一部のパフォーマンス目標を達成できませんでした" -ForegroundColor Yellow
    
    if ($uploadTime.TotalSeconds -gt 10) {
        Write-Host "  - CSV アップロード: $([math]::Round($uploadTime.TotalSeconds - 10, 2)) 秒オーバー" -ForegroundColor Red
    }
    if ($dataTime.TotalMilliseconds -gt 1000) {
        Write-Host "  - データ取得: $([math]::Round($dataTime.TotalMilliseconds - 1000, 0)) ミリ秒オーバー" -ForegroundColor Red
    }
    if ($analysisTime.TotalMilliseconds -gt 500) {
        Write-Host "  - 傾き算出: $([math]::Round($analysisTime.TotalMilliseconds - 500, 0)) ミリ秒オーバー" -ForegroundColor Red
    }
}
