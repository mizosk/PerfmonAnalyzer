namespace PerfmonAnalyzer.Api.Models;

/// <summary>
/// データ取得レスポンス
/// </summary>
public class DataResponse
{
    /// <summary>
    /// カウンタ情報のリスト
    /// </summary>
    public List<CounterInfo> Counters { get; set; } = [];
}
