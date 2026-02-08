# タスク003: CSV インポータの実装

**ステータス**: 未着手  
**優先度**: 高  
**見積もり**: 2時間

---

## 概要

Perfmon からエクスポートされた CSV ファイルを読み込み、内部データモデルに変換する機能を実装する。

## 前提条件

- タスク001（バックエンドセットアップ）が完了していること

## 作業内容

### 1. モデルクラス作成

`Models/` に以下のクラスを作成：

- `DataPoint.cs`: タイムスタンプと値のペア
- `CounterInfo.cs`: カウンタ情報とデータ配列
- `UploadResult.cs`: アップロード結果（セッションID、カウンタ一覧）

### 2. CsvImporter サービス実装

`Services/CsvImporter.cs`:

```csharp
public interface ICsvImporter
{
    Task<List<CounterInfo>> ImportAsync(Stream csvStream);
}
```

機能：
- エンコーディング自動判定（UTF-8 / Shift-JIS）
- ヘッダ行からカウンタ名を抽出
- タイムスタンプのパース
- 数値データの変換（欠損値は NaN として扱う）

### 3. FileController 実装

`Controllers/FileController.cs`:

```csharp
[HttpPost("upload")]
public async Task<ActionResult<UploadResult>> Upload(IFormFile file)
```

### 4. ユニットテスト作成

サンプル CSV を用いたテストケースを作成。

## 受け入れ基準

- [ ] Perfmon 形式の CSV を正しくパースできる
- [ ] UTF-8 と Shift-JIS の両方に対応
- [ ] 3,600行 × 12列を10秒以内に処理できる
- [ ] 欠損値（空セル）を適切に処理できる
- [ ] ユニットテストが通る

## 技術メモ

### CsvHelper の使い方

**CsvHelper** は .NET で CSV を扱うライブラリです。

```csharp
using CsvHelper;
using System.Globalization;

using var reader = new StreamReader(stream);
using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

// ヘッダ読み込み
csv.Read();
csv.ReadHeader();
var headers = csv.HeaderRecord;

// データ読み込み
while (csv.Read())
{
    var timestamp = csv.GetField<DateTime>(0);
    var value = csv.GetField<double>(1);
}
```

### エンコーディング判定

```csharp
// BOM でエンコーディングを判定
public static Encoding DetectEncoding(Stream stream)
{
    var bom = new byte[4];
    stream.Read(bom, 0, 4);
    stream.Position = 0;

    if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
        return Encoding.UTF8;
    
    // BOM なしの場合は Shift-JIS を試行
    return Encoding.GetEncoding("shift_jis");
}
```

### Perfmon CSV ヘッダの解析

ヘッダ例：
```
"\\SERVER\Process(app)\Private Bytes"
```

正規表現で分解：
```csharp
var pattern = @"\\\\[^\\]+\\Process\(([^)]+)\)\\(.+)";
// Group 1: プロセス名（app）
// Group 2: カウンタ種別（Private Bytes）
```

---

## 完了日

（完了時に記入）
