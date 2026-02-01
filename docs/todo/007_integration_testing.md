# タスク007: 統合テストとデバッグ

**ステータス**: 未着手  
**優先度**: 中  
**見積もり**: 2時間

---

## 概要

バックエンドとフロントエンドを結合し、エンドツーエンドの動作確認を行う。

## 前提条件

- タスク001〜006 が完了していること

## 作業内容

### 1. 開発環境での統合起動

2つのターミナルで同時に起動：

```powershell
# ターミナル1: バックエンド
cd src/backend/PerfmonAnalyzer.Api
dotnet run

# ターミナル2: フロントエンド
cd src/frontend
npm run dev
```

### 2. テストシナリオ実行

| シナリオ | 手順 | 期待結果 |
|----------|------|----------|
| CSV アップロード | テスト CSV をアップロード | グラフが表示される |
| 範囲選択 | 時間範囲を指定 | グラフが絞り込まれる |
| 傾き算出 | 「分析」ボタンをクリック | 傾きサマリが表示される |
| 閾値変更 | 閾値を30に変更 | 警告表示が更新される |
| 画像出力 | 「エクスポート」ボタンをクリック | PNG がダウンロードされる |

### 3. テスト用 CSV 作成

サンプルデータを作成（リーク傾向あり/なしの2パターン）。

```csv
"(PDH-CSV 4.0)","\\SERVER\Process(app1)\Private Bytes","\\SERVER\Process(app2)\Private Bytes"
"01/15/2026 10:00:00.000","10000000","20000000"
"01/15/2026 10:01:00.000","10050000","20000100"
...
```

### 4. バグ修正

発見した問題を修正。

### 5. パフォーマンス確認

60時間 × 1分 × 12列 相当のデータで応答時間を測定。

## 受け入れ基準

- [ ] エンドツーエンドのシナリオが全て成功する
- [ ] コンソールにエラーが出ない
- [ ] CSV 読み込みが10秒以内
- [ ] グラフ描画が1秒以内
- [ ] 傾き算出が500ms以内

## 技術メモ

### 開発時のデバッグ方法

**バックエンド（ASP.NET Core）**:
```powershell
# 詳細ログを有効化
dotnet run --environment Development
```

`appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

**フロントエンド（React）**:
- ブラウザの開発者ツール（F12）を使用
- `console.log()` でデバッグ出力
- React Developer Tools 拡張機能

### CORS エラーの対処

```
Access to XMLHttpRequest at 'https://localhost:5001' from origin 'http://localhost:5173' has been blocked by CORS policy
```

**解決策**: バックエンドの CORS 設定を確認

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### テストデータ生成スクリプト

```python
import csv
from datetime import datetime, timedelta

with open('test_data.csv', 'w', newline='') as f:
    writer = csv.writer(f)
    writer.writerow([
        '(PDH-CSV 4.0)',
        '\\\\SERVER\\Process(app1)\\Private Bytes',
        '\\\\SERVER\\Process(app2)\\Private Bytes'
    ])
    
    base_time = datetime(2026, 1, 15, 10, 0, 0)
    for i in range(3600):  # 60時間
        timestamp = (base_time + timedelta(minutes=i)).strftime('%m/%d/%Y %H:%M:%S.000')
        # app1: リークあり（毎分 5KB 増加）
        app1_value = 10000000 + i * 5120
        # app2: リークなし（変動のみ）
        app2_value = 20000000 + (i % 100) * 1000
        writer.writerow([timestamp, app1_value, app2_value])
```

---

## 完了日

（完了時に記入）
