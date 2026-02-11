namespace PerfmonAnalyzer.Api.Models;

/// <summary>
/// パフォーマンスカウンタの情報とデータポイント配列
/// </summary>
public class CounterInfo
{
    /// <summary>
    /// マシン名（例: SERVER）
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// カテゴリ名（例: Processor）
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// インスタンス名（例: _Total）。インスタンスなしの場合は空文字
    /// </summary>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>
    /// カウンタ名（例: % Processor Time）
    /// </summary>
    public string CounterName { get; set; } = string.Empty;

    /// <summary>
    /// 表示用の名前（ヘッダの生文字列）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// データポイントの配列
    /// </summary>
    public DataPoint[] DataPoints { get; set; } = [];
}
