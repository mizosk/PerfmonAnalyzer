using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// レポート出力形式の Strategy インターフェース。
/// 各出力形式（HTML, Markdown 等）はこのインターフェースを実装する。
/// </summary>
public interface IReportFormatStrategy
{
    /// <summary>
    /// 出力形式のコンテントタイプ（例: "text/html", "text/markdown"）
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// 出力ファイルの拡張子（例: "html", "md"）
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// レポートコンテンツを生成する
    /// </summary>
    /// <param name="context">レポート生成に必要なデータコンテキスト</param>
    /// <returns>生成されたレポートの文字列</returns>
    string Generate(ReportFormatContext context);
}

/// <summary>
/// Strategy に渡すレポート生成コンテキスト
/// </summary>
public class ReportFormatContext
{
    /// <summary>カウンタ情報のリスト</summary>
    public IReadOnlyList<CounterInfo> Counters { get; init; } = [];

    /// <summary>傾き分析結果のリスト</summary>
    public List<SlopeResult> SlopeResults { get; init; } = [];

    /// <summary>分析開始時刻</summary>
    public DateTime StartTime { get; init; }

    /// <summary>分析終了時刻</summary>
    public DateTime EndTime { get; init; }

    /// <summary>警告閾値（KB/10分）</summary>
    public double ThresholdKBPer10Min { get; init; }

    /// <summary>チャート画像のBase64文字列</summary>
    public string ChartImageBase64 { get; init; } = string.Empty;
}
