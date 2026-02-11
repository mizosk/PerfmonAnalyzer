namespace PerfmonAnalyzer.Api.Models;

/// <summary>
/// 傾き分析の結果
/// </summary>
public class SlopeResult
{
    /// <summary>
    /// カウンタの表示名
    /// </summary>
    public string CounterName { get; set; } = string.Empty;

    /// <summary>
    /// 傾き（KB/10分）
    /// </summary>
    public double SlopeKBPer10Min { get; set; }

    /// <summary>
    /// 閾値超過の警告フラグ
    /// </summary>
    public bool IsWarning { get; set; }

    /// <summary>
    /// 決定係数（R²）
    /// </summary>
    public double RSquared { get; set; }
}
