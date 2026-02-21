using System.Text;
using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// レポート生成サービスの実装
/// </summary>
public class ReportGenerator : IReportGenerator
{
    /// <inheritdoc />
    public ReportResponse GenerateReport(
        IReadOnlyList<CounterInfo> counters,
        List<SlopeResult> slopeResults,
        DateTime startTime,
        DateTime endTime,
        double thresholdKBPer10Min,
        string chartImageBase64,
        string format)
    {
        var normalizedFormat = format?.ToLowerInvariant() ?? "html";
        if (normalizedFormat != "md")
        {
            normalizedFormat = "html";
        }

        var content = normalizedFormat == "md"
            ? GenerateMarkdown(counters, slopeResults, startTime, endTime, thresholdKBPer10Min, chartImageBase64)
            : GenerateHtml(counters, slopeResults, startTime, endTime, thresholdKBPer10Min, chartImageBase64);

        var now = DateTime.Now;
        var fileName = $"perfmon_report_{now:yyyyMMdd_HHmmss}.{(normalizedFormat == "md" ? "md" : "html")}";
        var contentType = normalizedFormat == "md" ? "text/markdown" : "text/html";

        return new ReportResponse
        {
            Content = content,
            FileName = fileName,
            ContentType = contentType,
        };
    }

    /// <summary>
    /// HTML形式のレポートを生成する
    /// </summary>
    private static string GenerateHtml(
        IReadOnlyList<CounterInfo> counters,
        List<SlopeResult> slopeResults,
        DateTime startTime,
        DateTime endTime,
        double thresholdKBPer10Min,
        string chartImageBase64)
    {
        var warningCount = slopeResults.Count(r => r.IsWarning);
        var totalCount = slopeResults.Count;
        var warningResults = slopeResults.Where(r => r.IsWarning).ToList();

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
        sb.AppendLine($"<p><strong>分析期間:</strong> {startTime:yyyy-MM-dd HH:mm} ～ {endTime:yyyy-MM-dd HH:mm}</p>");
        sb.AppendLine($"<p><strong>閾値:</strong> {thresholdKBPer10Min} KB/10min</p>");
        sb.AppendLine("</div>");

        // サマリ
        sb.AppendLine("<h2>サマリ</h2>");
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<p>総カウンタ数: {totalCount}</p>");
        sb.AppendLine($"<p>警告カウンタ数: {warningCount}</p>");
        sb.AppendLine("</div>");

        // グラフ
        sb.AppendLine("<h2>グラフ</h2>");
        if (!string.IsNullOrEmpty(chartImageBase64))
        {
            var imgSrc = chartImageBase64.StartsWith("data:") ? chartImageBase64 : $"data:image/png;base64,{chartImageBase64}";
            if (IsValidBase64ImageSrc(imgSrc))
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

        // カウンタ別詳細
        sb.AppendLine("<h2>カウンタ別詳細</h2>");
        if (slopeResults.Count > 0)
        {
            sb.AppendLine("<table>");
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr><th>プロセス名</th><th>カウンタ名</th><th>傾き (KB/10min)</th><th>R²</th><th>判定</th></tr>");
            sb.AppendLine("</thead>");
            sb.AppendLine("<tbody>");

            foreach (var slope in slopeResults)
            {
                var (processName, counterName) = ParseCounterName(slope.CounterName);
                var rowClass = slope.IsWarning ? " class=\"warning\"" : "";
                var status = slope.IsWarning ? "⚠ 警告" : "✓ 正常";
                sb.AppendLine($"<tr{rowClass}><td>{Escape(processName)}</td><td>{Escape(counterName)}</td><td>{slope.SlopeKBPer10Min:F2}</td><td>{slope.RSquared:F4}</td><td>{status}</td></tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
        }
        else
        {
            sb.AppendLine("<p>カウンタデータがありません。</p>");
        }

        // 閾値超過カウンタ一覧
        sb.AppendLine("<h2>閾値超過カウンタ一覧</h2>");
        if (warningResults.Count > 0)
        {
            sb.AppendLine("<table class=\"warning-table\">");
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr><th>プロセス名</th><th>カウンタ名</th><th>傾き (KB/10min)</th></tr>");
            sb.AppendLine("</thead>");
            sb.AppendLine("<tbody>");

            foreach (var slope in warningResults)
            {
                var (processName, counterName) = ParseCounterName(slope.CounterName);
                sb.AppendLine($"<tr class=\"warning\"><td>{Escape(processName)}</td><td>{Escape(counterName)}</td><td>{slope.SlopeKBPer10Min:F2}</td></tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
        }
        else
        {
            sb.AppendLine("<p>閾値を超過したカウンタはありません。</p>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// Markdown形式のレポートを生成する
    /// </summary>
    private static string GenerateMarkdown(
        IReadOnlyList<CounterInfo> counters,
        List<SlopeResult> slopeResults,
        DateTime startTime,
        DateTime endTime,
        double thresholdKBPer10Min,
        string chartImageBase64)
    {
        var warningCount = slopeResults.Count(r => r.IsWarning);
        var totalCount = slopeResults.Count;
        var warningResults = slopeResults.Where(r => r.IsWarning).ToList();

        var sb = new StringBuilder();

        // タイトル
        sb.AppendLine("# Perfmon Analyzer レポート");
        sb.AppendLine();
        sb.AppendLine($"- **生成日時:** {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"- **分析期間:** {startTime:yyyy-MM-dd HH:mm} ～ {endTime:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"- **閾値:** {thresholdKBPer10Min} KB/10min");
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
        if (!string.IsNullOrEmpty(chartImageBase64))
        {
            var imgSrc = chartImageBase64.StartsWith("data:") ? chartImageBase64 : $"data:image/png;base64,{chartImageBase64}";
            if (IsValidBase64ImageSrc(imgSrc))
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
        sb.AppendLine();

        // カウンタ別詳細
        sb.AppendLine("## カウンタ別詳細");
        sb.AppendLine();
        if (slopeResults.Count > 0)
        {
            sb.AppendLine("| プロセス名 | カウンタ名 | 傾き (KB/10min) | R² | 判定 |");
            sb.AppendLine("|---|---|---|---|---|");

            foreach (var slope in slopeResults)
            {
                var (processName, counterName) = ParseCounterName(slope.CounterName);
                var status = slope.IsWarning ? "⚠ 警告" : "✓ 正常";
                sb.AppendLine($"| {processName} | {counterName} | {slope.SlopeKBPer10Min:F2} | {slope.RSquared:F4} | {status} |");
            }
        }
        else
        {
            sb.AppendLine("カウンタデータがありません。");
        }
        sb.AppendLine();

        // 閾値超過カウンタ一覧
        sb.AppendLine("## 閾値超過カウンタ一覧");
        sb.AppendLine();
        if (warningResults.Count > 0)
        {
            sb.AppendLine("| プロセス名 | カウンタ名 | 傾き (KB/10min) |");
            sb.AppendLine("|---|---|---|");

            foreach (var slope in warningResults)
            {
                var (processName, counterName) = ParseCounterName(slope.CounterName);
                sb.AppendLine($"| {processName} | {counterName} | {slope.SlopeKBPer10Min:F2} |");
            }
        }
        else
        {
            sb.AppendLine("閾値を超過したカウンタはありません。");
        }

        return sb.ToString();
    }

    /// <summary>
    /// カウンタの表示名からプロセス名とカウンタ名を解析する
    /// 例: "\\\\SERVER\\Process(Process0)\\Working Set - Private"
    /// → ("Process0", "Working Set - Private")
    /// </summary>
    private static (string ProcessName, string CounterName) ParseCounterName(string displayName)
    {
        // パターン: \\Machine\Category(Instance)\CounterName
        var parts = displayName.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
        {
            var categoryInstance = parts[1];
            var counterName = parts[2];

            // Category(Instance) からインスタンス名を抽出
            var parenStart = categoryInstance.IndexOf('(');
            var parenEnd = categoryInstance.IndexOf(')');
            if (parenStart >= 0 && parenEnd > parenStart)
            {
                var processName = categoryInstance.Substring(parenStart + 1, parenEnd - parenStart - 1);
                return (processName, counterName);
            }

            return (categoryInstance, counterName);
        }

        return (displayName, displayName);
    }

    /// <summary>
    /// Base64画像のdata URIが安全なパターンにマッチするか検証する
    /// </summary>
    private static bool IsValidBase64ImageSrc(string src)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            src,
            @"^data:image/(png|jpeg|gif|svg\+xml);base64,[A-Za-z0-9+/=]+$");
    }

    /// <summary>
    /// HTML特殊文字をエスケープする
    /// </summary>
    private static string Escape(string value)
    {
        return System.Net.WebUtility.HtmlEncode(value);
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
