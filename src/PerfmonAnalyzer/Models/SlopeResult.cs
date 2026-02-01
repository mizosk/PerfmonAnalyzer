namespace PerfmonAnalyzer.Models;

/// <summary>
/// 傾き算出結果
/// </summary>
public class SlopeResult
{
    /// <summary>
    /// カウンタ名
    /// </summary>
    public string CounterName { get; set; } = string.Empty;

    /// <summary>
    /// 傾き（KB/10min）
    /// </summary>
    public double SlopeKBPer10Min { get; set; }

    /// <summary>
    /// 閾値超過フラグ
    /// </summary>
    public bool IsWarning { get; set; }

    /// <summary>
    /// 決定係数（参考）
    /// </summary>
    public double RSquared { get; set; }
}
