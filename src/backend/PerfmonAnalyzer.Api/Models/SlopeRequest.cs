namespace PerfmonAnalyzer.Api.Models;

/// <summary>
/// 傾き分析のリクエスト
/// </summary>
public class SlopeRequest
{
    /// <summary>
    /// セッション識別子
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 分析開始時刻
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 分析終了時刻
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 警告閾値（KB/10分）。既定値は 50。
    /// </summary>
    public double ThresholdKBPer10Min { get; set; } = 50;
}
