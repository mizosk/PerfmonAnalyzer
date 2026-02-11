namespace PerfmonAnalyzer.Api.Models;

/// <summary>
/// セッション情報を保持するモデル
/// </summary>
public class SessionInfo
{
    /// <summary>
    /// セッション識別子
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// セッション作成日時
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最終アクセス日時
    /// </summary>
    public DateTime LastAccessedAt { get; set; }

    /// <summary>
    /// パース済みカウンタデータ
    /// </summary>
    public List<CounterInfo> Counters { get; set; } = [];
}
