namespace PerfmonAnalyzer.Api.Models;

/// <summary>
/// 傾き分析のレスポンス
/// </summary>
public class SlopeResponse
{
    /// <summary>
    /// 分析結果のリスト
    /// </summary>
    public List<SlopeResult> Results { get; set; } = [];
}
