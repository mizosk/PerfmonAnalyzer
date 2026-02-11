# パフォーマンスモニタのテストデータを生成するPowerShellスクリプト

param(
    [int]$Hours = 60,
    [string]$OutputFile = "test_data_60h.csv"
)

Write-Host "テストデータを生成中..." -ForegroundColor Cyan

# CSVの内容を構築
$csv = New-Object System.Collections.ArrayList

# ヘッダー行
$null = $csv.Add('"(PDH-CSV 4.0)","\\SERVER\Process(app1)\Private Bytes","\\SERVER\Process(app2)\Private Bytes","\\SERVER\Process(app3)\Private Bytes"')

# 開始時刻
$baseTime = Get-Date -Year 2026 -Month 1 -Day 15 -Hour 10 -Minute 0 -Second 0

# データポイント数（1分間隔）
$dataPoints = $Hours * 60

for ($i = 0; $i -lt $dataPoints; $i++) {
    $timestamp = $baseTime.AddMinutes($i).ToString('MM/dd/yyyy HH:mm:ss.fff')
    
    # app1: メモリリークあり（毎分 5KB 増加）
    $app1Value = 10000000 + $i * 5120
    
    # app2: リークなし（変動のみ）
    $random = Get-Random -Minimum -50000 -Maximum 50000
    $app2Value = 20000000 + $random
    
    # app3: 段階的なリーク（30分ごとに大きく増加）
    $step = [Math]::Floor($i / 30)
    $random2 = Get-Random -Minimum -10000 -Maximum 10000
    $app3Value = 15000000 + $step * 100000 + $random2
    
    $null = $csv.Add("""$timestamp"",""$app1Value"",""$app2Value"",""$app3Value""")
}

# ファイルに書き込み
$csv | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host "✓ $OutputFile を作成しました ($dataPoints データポイント)" -ForegroundColor Green

# 小さなテストデータも生成
Write-Host "`n小さなテストデータを生成中..." -ForegroundColor Cyan

$csvSmall = New-Object System.Collections.ArrayList
$null = $csvSmall.Add('"(PDH-CSV 4.0)","\\SERVER\Process(test1)\Private Bytes","\\SERVER\Process(test2)\Private Bytes"')

$baseTime = Get-Date -Year 2026 -Month 2 -Day 11 -Hour 10 -Minute 0 -Second 0

for ($i = 0; $i -lt 30; $i++) {
    $timestamp = $baseTime.AddMinutes($i).ToString('MM/dd/yyyy HH:mm:ss.fff')
    $test1Value = 5000000 + $i * 10240
    $test2Value = 8000000 + ($i % 5) * 5000
    
    $null = $csvSmall.Add("""$timestamp"",""$test1Value"",""$test2Value""")
}

$csvSmall | Out-File -FilePath "test_data_small.csv" -Encoding UTF8

Write-Host "✓ test_data_small.csv を作成しました (30 データポイント)" -ForegroundColor Green
Write-Host "`n全てのテストデータを生成しました！" -ForegroundColor Green
