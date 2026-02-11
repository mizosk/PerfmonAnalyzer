namespace PerfmonAnalyzer.Api.Models;

/// <summary>
/// CSVアップロード結果
/// </summary>
public class UploadResult
{
    /// <summary>
    /// セッション識別子
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// パースされたカウンタ情報の一覧
    /// </summary>
    public List<CounterInfo> Counters { get; set; } = [];
}
