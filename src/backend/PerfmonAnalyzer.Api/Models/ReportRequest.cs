namespace PerfmonAnalyzer.Api.Models;

/// <summary>
/// レポート生成リクエストDTO
/// </summary>
public class ReportRequest
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

    /// <summary>
    /// チャート画像の Base64 エンコード文字列
    /// </summary>
    public string ChartImageBase64 { get; set; } = string.Empty;

    /// <summary>
    /// 出力形式（"html" または "md"）
    /// </summary>
    public string Format { get; set; } = "html";
}
