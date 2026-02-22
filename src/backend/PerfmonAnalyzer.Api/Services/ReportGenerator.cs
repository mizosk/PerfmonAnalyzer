using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// レポート生成サービスの実装。
/// Strategy パターンにより、出力形式ごとの生成ロジックを IReportFormatStrategy に委譲する。
/// </summary>
public class ReportGenerator : IReportGenerator
{
    /// <summary>
    /// フォーマット文字列と Strategy のマッピング（キャッシュ）
    /// </summary>
    private static readonly Dictionary<string, IReportFormatStrategy> Strategies = new()
    {
        ["html"] = new HtmlReportStrategy(),
        ["md"] = new MarkdownReportStrategy(),
    };

    /// <summary>
    /// デフォルトの Strategy（HTML）
    /// </summary>
    private static readonly IReportFormatStrategy DefaultStrategy = Strategies["html"];

    /// <inheritdoc />
    public ReportResponse GenerateReport(
        IReadOnlyList<CounterInfo> counters,
        IReadOnlyList<SlopeResult> slopeResults,
        DateTime startTime,
        DateTime endTime,
        double thresholdKBPer10Min,
        string chartImageBase64,
        string format)
    {
        var strategy = GetStrategy(format);
        var now = DateTime.Now;

        var context = new ReportFormatContext
        {
            Counters = counters,
            SlopeResults = slopeResults,
            GeneratedAt = now,
            StartTime = startTime,
            EndTime = endTime,
            ThresholdKBPer10Min = thresholdKBPer10Min,
            ChartImageBase64 = chartImageBase64 ?? string.Empty,
        };

        var content = strategy.Generate(context);
        var fileName = $"perfmon_report_{now:yyyyMMdd_HHmmss}.{strategy.FileExtension}";

        return new ReportResponse
        {
            Content = content,
            FileName = fileName,
            ContentType = strategy.ContentType,
        };
    }

    /// <summary>
    /// フォーマット文字列から対応する Strategy を取得する。
    /// 未知のフォーマットの場合は HTML をデフォルトとして返す。
    /// </summary>
    private static IReportFormatStrategy GetStrategy(string? format)
    {
        var normalizedFormat = format?.ToLowerInvariant() ?? "html";
        return Strategies.GetValueOrDefault(normalizedFormat, DefaultStrategy);
    }
}
