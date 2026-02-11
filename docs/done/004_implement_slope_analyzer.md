# タスク004: 傾き分析機能の実装

**ステータス**: 完了  
**優先度**: 高  
**見積もり**: 2時間

---

## 概要

時系列データに対して線形回帰（最小二乗法）を適用し、傾きを算出する機能を実装する。

## 前提条件

- タスク003（CSV インポータ）が完了していること

## 作業内容

### 1. モデルクラス作成

`Models/SlopeResult.cs`:

```csharp
public class SlopeResult
{
    public string CounterName { get; set; }
    public double SlopeKBPer10Min { get; set; }
    public bool IsWarning { get; set; }
    public double RSquared { get; set; }
}

public class SlopeRequest
{
    public string SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double ThresholdKBPer10Min { get; set; } = 50;
}
```

### 2. SlopeAnalyzer サービス実装

`Services/SlopeAnalyzer.cs`:

```csharp
public interface ISlopeAnalyzer
{
    List<SlopeResult> Calculate(
        List<CounterInfo> counters,
        DateTime startTime,
        DateTime endTime,
        double thresholdKBPer10Min
    );
}
```

### 3. 線形回帰アルゴリズム実装

最小二乗法（OLS）による傾き算出：

```csharp
public static (double slope, double rSquared) LinearRegression(
    List<DataPoint> data)
{
    int n = data.Count;
    double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
    
    // 時間を分単位の数値に変換（基準点からの経過分）
    var baseTime = data[0].Timestamp;
    
    foreach (var point in data)
    {
        double x = (point.Timestamp - baseTime).TotalMinutes;
        double y = point.Value;
        
        sumX += x;
        sumY += y;
        sumXY += x * y;
        sumX2 += x * x;
    }
    
    double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
    
    // R² 算出（省略）
    
    return (slope, rSquared);
}
```

### 4. 単位変換

```csharp
// bytes/min → KB/10min
double slopeKBPer10Min = slopeBytesPerMin * 10 / 1024;
```

### 5. AnalysisController 実装

`Controllers/AnalysisController.cs`:

```csharp
[HttpPost("slope")]
public ActionResult<SlopeResponse> CalculateSlope([FromBody] SlopeRequest request)
```

### 6. ユニットテスト作成

既知の傾きを持つテストデータで検証。

## 受け入れ基準

- [x] 傾きが KB/10min 単位で正しく算出される
- [x] 閾値超過時に `IsWarning = true` となる
- [x] 決定係数（R²）が算出される
- [x] 3,600ポイントの計算が500ms以内に完了
- [x] ユニットテストが通る

## 技術メモ

### 最小二乗法（OLS）とは

**最小二乗法**は、データ点と近似直線の誤差（残差）の二乗和を最小化する手法です。

データ $(x_i, y_i)$ に対して直線 $y = ax + b$ をフィットさせるとき：

$$
a = \frac{n\sum x_i y_i - \sum x_i \sum y_i}{n\sum x_i^2 - (\sum x_i)^2}
$$

$$
b = \frac{\sum y_i - a \sum x_i}{n}
$$

### 決定係数（R²）とは

**決定係数**は回帰直線のフィット精度を表す指標です。

- **R² = 1**: 完全にフィット（全点が直線上）
- **R² = 0**: フィットしない（平均値と同程度）
- **0 < R² < 1**: 通常の範囲

メモリリークの場合、R² が高いほど「安定して増加している」ことを示します。

### 傾きの解釈

| 傾き（KB/10min） | 解釈 |
|------------------|------|
| < 0 | メモリ減少（GC などによる） |
| 0 〜 10 | 正常範囲（変動） |
| 10 〜 50 | 要観察 |
| > 50 | **警告**（リーク疑い） |

---

## 完了日

2026-02-11
