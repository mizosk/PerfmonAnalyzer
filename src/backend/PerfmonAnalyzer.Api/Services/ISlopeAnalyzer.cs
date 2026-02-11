using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// 傾き分析サービスのインターフェース
/// </summary>
public interface ISlopeAnalyzer
{
    /// <summary>
    /// 指定された時間範囲のカウンタデータに対して線形回帰を行い、傾きを算出する
    /// </summary>
    /// <param name="counters">カウンタ情報のリスト</param>
    /// <param name="startTime">分析開始時刻</param>
    /// <param name="endTime">分析終了時刻</param>
    /// <param name="thresholdKBPer10Min">警告閾値（KB/10分）</param>
    /// <returns>各カウンタの傾き分析結果</returns>
    List<SlopeResult> Calculate(
        IReadOnlyList<CounterInfo> counters,
        DateTime startTime,
        DateTime endTime,
        double thresholdKBPer10Min);
}
