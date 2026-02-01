namespace PerfmonAnalyzer.Models;

/// <summary>
/// 時系列データポイント
/// </summary>
public class DataPoint
{
    /// <summary>
    /// タイムスタンプ
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 値
    /// </summary>
    public double Value { get; set; }
}
