namespace PerfmonAnalyzer.Api.Models;

/// <summary>
/// レポート生成レスポンスDTO
/// </summary>
public class ReportResponse
{
    /// <summary>
    /// レポートの内容（HTML または Markdown テキスト）
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// ダウンロード用ファイル名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// コンテンツタイプ（例: text/html, text/markdown）
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}
