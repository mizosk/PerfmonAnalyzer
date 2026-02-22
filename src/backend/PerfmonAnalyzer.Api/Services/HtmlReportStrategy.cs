using System.Text;
using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// HTML形式のレポート生成 Strategy
/// </summary>
public class HtmlReportStrategy : IReportFormatStrategy
{
    /// <inheritdoc />
    public string ContentType => "text/html";

    /// <inheritdoc />
    public string FileExtension => "html";

    /// <inheritdoc />
    public string Generate(ReportFormatContext context)
    {
        var warningCount = context.SlopeResults.Count(r => r.IsWarning);
        var totalCount = context.SlopeResults.Count;
        var warningResults = context.SlopeResults.Where(r => r.IsWarning).ToList();

        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"ja\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<title>Perfmon Analyzer レポート</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCssStyles());
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // タイトル
        sb.AppendLine("<h1>Perfmon Analyzer レポート</h1>");

        // メタ情報
        sb.AppendLine("<div class=\"meta-info\">");
        sb.AppendLine($"<p><strong>生成日時:</strong> {DateTime.Now:yyyy-MM-dd HH:mm}</p>");
        sb.AppendLine($"<p><strong>分析期間:</strong> {context.StartTime:yyyy-MM-dd HH:mm} ～ {context.EndTime:yyyy-MM-dd HH:mm}</p>");
        sb.AppendLine($"<p><strong>閾値:</strong> {context.ThresholdKBPer10Min} KB/10min</p>");
        sb.AppendLine("</div>");

        // サマリ
        sb.AppendLine("<h2>サマリ</h2>");
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<p>総カウンタ数: {totalCount}</p>");
        sb.AppendLine($"<p>警告カウンタ数: {warningCount}</p>");
        sb.AppendLine("</div>");

        // グラフ
        sb.AppendLine("<h2>グラフ</h2>");
        AppendChartSection(sb, context.ChartImageBase64);

        // カウンタ別詳細
        sb.AppendLine("<h2>カウンタ別詳細</h2>");
        AppendDetailTable(sb, context.SlopeResults);

        // 閾値超過カウンタ一覧
        sb.AppendLine("<h2>閾値超過カウンタ一覧</h2>");
        AppendWarningTable(sb, warningResults);

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

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
                sb.AppendLine($"<div class=\"chart\"><img src=\"{imgSrc}\" alt=\"パフォーマンスグラフ\" /></div>");
            }
            else
            {
                sb.AppendLine("<p>グラフ画像の形式が不正です。</p>");
            }
        }
        else
        {
            sb.AppendLine("<p>グラフ画像が提供されていません。</p>");
        }
    }

    /// <summary>
    /// カウンタ別詳細テーブルを出力する
    /// </summary>
    private static void AppendDetailTable(StringBuilder sb, List<SlopeResult> slopeResults)
    {
        if (slopeResults.Count > 0)
        {
            sb.AppendLine("<table>");
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr><th>プロセス名</th><th>カウンタ名</th><th>傾き (KB/10min)</th><th>R²</th><th>判定</th></tr>");
            sb.AppendLine("</thead>");
            sb.AppendLine("<tbody>");

            foreach (var slope in slopeResults)
            {
                var (processName, counterName) = ReportUtilities.ParseCounterName(slope.CounterName);
                var rowClass = slope.IsWarning ? " class=\"warning\"" : "";
                var status = slope.IsWarning ? "⚠ 警告" : "✓ 正常";
                sb.AppendLine($"<tr{rowClass}><td>{ReportUtilities.HtmlEscape(processName)}</td><td>{ReportUtilities.HtmlEscape(counterName)}</td><td>{slope.SlopeKBPer10Min:F2}</td><td>{slope.RSquared:F4}</td><td>{status}</td></tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
        }
        else
        {
            sb.AppendLine("<p>カウンタデータがありません。</p>");
        }
    }

    /// <summary>
    /// 閾値超過カウンタ一覧テーブルを出力する
    /// </summary>
    private static void AppendWarningTable(StringBuilder sb, List<SlopeResult> warningResults)
    {
        if (warningResults.Count > 0)
        {
            sb.AppendLine("<table class=\"warning-table\">");
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr><th>プロセス名</th><th>カウンタ名</th><th>傾き (KB/10min)</th></tr>");
            sb.AppendLine("</thead>");
            sb.AppendLine("<tbody>");

            foreach (var slope in warningResults)
            {
                var (processName, counterName) = ReportUtilities.ParseCounterName(slope.CounterName);
                sb.AppendLine($"<tr class=\"warning\"><td>{ReportUtilities.HtmlEscape(processName)}</td><td>{ReportUtilities.HtmlEscape(counterName)}</td><td>{slope.SlopeKBPer10Min:F2}</td></tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
        }
        else
        {
            sb.AppendLine("<p>閾値を超過したカウンタはありません。</p>");
        }
    }

    /// <summary>
    /// インラインCSSスタイルを返す
    /// </summary>
    private static string GetCssStyles()
    {
        return """
            body {
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                max-width: 1200px;
                margin: 0 auto;
                padding: 20px;
                background-color: #f5f5f5;
                color: #333;
            }
            h1 {
                color: #2c3e50;
                border-bottom: 3px solid #3498db;
                padding-bottom: 10px;
            }
            h2 {
                color: #2980b9;
                margin-top: 30px;
            }
            .meta-info {
                background-color: #ecf0f1;
                padding: 15px;
                border-radius: 5px;
                margin-bottom: 20px;
            }
            .summary {
                background-color: #fff;
                padding: 15px;
                border-radius: 5px;
                border: 1px solid #ddd;
            }
            .chart img {
                max-width: 100%;
                border: 1px solid #ddd;
                border-radius: 5px;
            }
            table {
                width: 100%;
                border-collapse: collapse;
                margin-top: 10px;
                background-color: #fff;
            }
            th, td {
                border: 1px solid #ddd;
                padding: 10px;
                text-align: left;
            }
            th {
                background-color: #3498db;
                color: #fff;
            }
            tr:nth-child(even) {
                background-color: #f9f9f9;
            }
            tr.warning {
                background-color: #ffe0e0;
                color: #c0392b;
                font-weight: bold;
            }
            .warning-table tr.warning {
                background-color: #ffe0e0;
            }
            """;
    }
}
