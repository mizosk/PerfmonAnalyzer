namespace PerfmonAnalyzer.Api.Models;

/// <summary>
/// タイムスタンプと値のペアを表すデータポイント
/// </summary>
public class DataPoint
{
    /// <summary>
    /// 計測日時
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 計測値（欠損時は double.NaN）
    /// </summary>
    public double Value { get; set; }
}
