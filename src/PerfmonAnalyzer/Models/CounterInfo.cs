namespace PerfmonAnalyzer.Models;

/// <summary>
/// カウンタ情報
/// </summary>
public class CounterInfo
{
    /// <summary>
    /// カウンタ名（例: "Process(app)\Private Bytes"）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// プロセス名（例: "app"）
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// カウンタタイプ（例: "Private Bytes"）
    /// </summary>
    public string CounterType { get; set; } = string.Empty;

    /// <summary>
    /// データポイントリスト
    /// </summary>
    public List<DataPoint> Data { get; set; } = new();
}
