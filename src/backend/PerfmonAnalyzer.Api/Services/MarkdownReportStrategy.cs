using System.Text;
using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// Markdown形式のレポート生成 Strategy
/// </summary>
public class MarkdownReportStrategy : IReportFormatStrategy
{
    /// <inheritdoc />
    public string ContentType => "text/markdown";

    /// <inheritdoc />
    public string FileExtension => "md";

    /// <inheritdoc />
    public string Generate(ReportFormatContext context)
    {
        var warningCount = context.SlopeResults.Count(r => r.IsWarning);
        var totalCount = context.SlopeResults.Count;
        var warningResults = context.SlopeResults.Where(r => r.IsWarning).ToList();

        var sb = new StringBuilder();

        // タイトル
        sb.AppendLine("# Perfmon Analyzer レポート");
        sb.AppendLine();
        sb.AppendLine($"- **生成日時:** {context.GeneratedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"- **分析期間:** {context.StartTime:yyyy-MM-dd HH:mm} ～ {context.EndTime:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"- **閾値:** {context.ThresholdKBPer10Min} KB/10min");
        sb.AppendLine();

        // サマリ
        sb.AppendLine("## サマリ");
        sb.AppendLine();
        sb.AppendLine($"- 総カウンタ数: {totalCount}");
        sb.AppendLine($"- 警告カウンタ数: {warningCount}");
        sb.AppendLine();

        // グラフ
        sb.AppendLine("## グラフ");
        sb.AppendLine();
        AppendChartSection(sb, context.ChartImageBase64);
        sb.AppendLine();

        // カウンタ別詳細
        sb.AppendLine("## カウンタ別詳細");
        sb.AppendLine();
        AppendDetailTable(sb, context.SlopeResults);
        sb.AppendLine();

        // 閾値超過カウンタ一覧
        sb.AppendLine("## 閾値超過カウンタ一覧");
        sb.AppendLine();
        AppendWarningTable(sb, warningResults);

        return sb.ToString();
    }

    /// <summary>
    /// グラフセクションを出力する
    /// </summary>
    private static void AppendChartSection(StringBuilder sb, string chartImageBase64)
    {
        if (!string.IsNullOrEmpty(chartImageBase64))
        {
            var imgSrc = ReportUtilities.ToDataUri(chartImageBase64);
            if (ReportUtilities.IsValidBase64ImageSrc(imgSrc))
            {
                sb.AppendLine($"![パフォーマンスグラフ]({imgSrc})");
            }
            else
            {
                sb.AppendLine("グラフ画像の形式が不正です。");
            }
        }
        else
        {
            sb.AppendLine("グラフ画像が提供されていません。");
        }
    }

    /// <summary>
    /// カウンタ別詳細テーブルを出力する
    /// </summary>
    private static void AppendDetailTable(StringBuilder sb, IReadOnlyList<SlopeResult> slopeResults)
    {
        if (slopeResults.Count > 0)
        {
            sb.AppendLine("| プロセス名 | カウンタ名 | 傾き (KB/10min) | R² | 判定 |");
            sb.AppendLine("|---|---|---|---|---|");

            foreach (var slope in slopeResults)
            {
                var (processName, counterName) = ReportUtilities.ParseCounterName(slope.CounterName);
                var status = slope.IsWarning ? "⚠ 警告" : "✓ 正常";
                sb.AppendLine($"| {processName} | {counterName} | {slope.SlopeKBPer10Min:F2} | {slope.RSquared:F4} | {status} |");
            }
        }
        else
        {
            sb.AppendLine("カウンタデータがありません。");
        }
    }

    /// <summary>
    /// 閾値超過カウンタ一覧テーブルを出力する
    /// </summary>
    private static void AppendWarningTable(StringBuilder sb, IReadOnlyList<SlopeResult> warningResults)
    {
        if (warningResults.Count > 0)
        {
            sb.AppendLine("| プロセス名 | カウンタ名 | 傾き (KB/10min) |");
            sb.AppendLine("|---|---|---|");

            foreach (var slope in warningResults)
            {
                var (processName, counterName) = ReportUtilities.ParseCounterName(slope.CounterName);
                sb.AppendLine($"| {processName} | {counterName} | {slope.SlopeKBPer10Min:F2} |");
            }
        }
        else
        {
            sb.AppendLine("閾値を超過したカウンタはありません。");
        }
    }
}
