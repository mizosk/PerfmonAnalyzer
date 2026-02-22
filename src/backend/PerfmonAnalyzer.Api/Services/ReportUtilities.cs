using System.Text.RegularExpressions;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// レポート生成で共通利用するユーティリティメソッド群
/// </summary>
internal static class ReportUtilities
{
    /// <summary>
    /// Base64画像の data URI パターン
    /// </summary>
    private static readonly Regex Base64ImagePattern = new(
        @"^data:image/(png|jpeg|gif|svg\+xml);base64,[A-Za-z0-9+/=]+$",
        RegexOptions.Compiled);

    /// <summary>
    /// カウンタの表示名からプロセス名とカウンタ名を解析する。
    /// 例: "\\\\SERVER\\Process(Process0)\\Working Set - Private"
    /// → ("Process0", "Working Set - Private")
    /// </summary>
    public static (string ProcessName, string CounterName) ParseCounterName(string displayName)
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
    /// Base64画像の data URI が安全なパターンにマッチするか検証する
    /// </summary>
    public static bool IsValidBase64ImageSrc(string src)
    {
        return Base64ImagePattern.IsMatch(src);
    }

    /// <summary>
    /// HTML特殊文字をエスケープする
    /// </summary>
    public static string HtmlEscape(string value)
    {
        return System.Net.WebUtility.HtmlEncode(value);
    }

    /// <summary>
    /// チャート画像の Base64 文字列を data URI 形式に変換する。
    /// 既に data: プレフィックスを持つ場合はそのまま返す。
    /// </summary>
    public static string ToDataUri(string chartImageBase64)
    {
        return chartImageBase64.StartsWith("data:")
            ? chartImageBase64
            : $"data:image/png;base64,{chartImageBase64}";
    }
}
