using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// レポート生成サービスのインターフェース
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// カウンタデータと傾き分析結果からレポートを生成する
    /// </summary>
    /// <param name="counters">カウンタ情報のリスト</param>
    /// <param name="slopeResults">傾き分析結果のリスト</param>
    /// <param name="startTime">分析開始時刻</param>
    /// <param name="endTime">分析終了時刻</param>
    /// <param name="thresholdKBPer10Min">警告閾値（KB/10分）</param>
    /// <param name="chartImageBase64">チャート画像のBase64文字列</param>
    /// <param name="format">出力形式（"html" または "md"）</param>
    /// <returns>生成されたレポート</returns>
    ReportResponse GenerateReport(
        IReadOnlyList<CounterInfo> counters,
        IReadOnlyList<SlopeResult> slopeResults,
        DateTime startTime,
        DateTime endTime,
        double thresholdKBPer10Min,
        string chartImageBase64,
        string format);
}
