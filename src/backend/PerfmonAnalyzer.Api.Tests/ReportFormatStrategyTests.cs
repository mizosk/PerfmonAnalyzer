using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Tests;

/// <summary>
/// HtmlReportStrategy と MarkdownReportStrategy の個別テスト
/// </summary>
public class ReportFormatStrategyTests
{
    #region ヘルパーメソッド

    private static ReportFormatContext CreateTestContext(
        int slopeCount = 3,
        bool withWarning = true,
        string chartImage = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUg==")
    {
        var slopeResults = new List<SlopeResult>();
        for (int i = 0; i < slopeCount; i++)
        {
            slopeResults.Add(new SlopeResult
            {
                CounterName = $"\\\\SERVER\\Process(Process{i})\\Working Set - Private",
                SlopeKBPer10Min = withWarning && i == 0 ? 80.5 : 10.0 + i,
                IsWarning = withWarning && i == 0,
                RSquared = 0.95 + i * 0.01,
            });
        }

        return new ReportFormatContext
        {
            Counters = [],
            SlopeResults = slopeResults,
            StartTime = new DateTime(2026, 1, 1, 0, 0, 0),
            EndTime = new DateTime(2026, 1, 1, 0, 20, 0),
            ThresholdKBPer10Min = 50.0,
            ChartImageBase64 = chartImage,
        };
    }

    #endregion

    #region HtmlReportStrategy テスト

    [Fact]
    public void HtmlStrategy_プロパティが正しい()
    {
        var strategy = new HtmlReportStrategy();

        Assert.Equal("text/html", strategy.ContentType);
        Assert.Equal("html", strategy.FileExtension);
    }

    [Fact]
    public void HtmlStrategy_HTML構造を生成する()
    {
        var strategy = new HtmlReportStrategy();
        var context = CreateTestContext();

        var result = strategy.Generate(context);

        Assert.Contains("<!DOCTYPE html>", result);
        Assert.Contains("<html", result);
        Assert.Contains("</html>", result);
        Assert.Contains("<style>", result);
    }

    [Fact]
    public void HtmlStrategy_警告行にwarningクラスを付与する()
    {
        var strategy = new HtmlReportStrategy();
        var context = CreateTestContext(withWarning: true);

        var result = strategy.Generate(context);

        Assert.Contains("class=\"warning\"", result);
        Assert.Contains("⚠ 警告", result);
    }

    [Fact]
    public void HtmlStrategy_空のカウンタでもエラーなく生成する()
    {
        var strategy = new HtmlReportStrategy();
        var context = CreateTestContext(slopeCount: 0);

        var result = strategy.Generate(context);

        Assert.Contains("カウンタデータがありません", result);
        Assert.Contains("閾値を超過したカウンタはありません", result);
    }

    [Fact]
    public void HtmlStrategy_不正なBase64でフォールバックメッセージを出す()
    {
        var strategy = new HtmlReportStrategy();
        var context = CreateTestContext(chartImage: "javascript:alert('xss')");

        var result = strategy.Generate(context);

        Assert.DoesNotContain("<img", result);
        Assert.Contains("グラフ画像の形式が不正です", result);
    }

    #endregion

    #region MarkdownReportStrategy テスト

    [Fact]
    public void MdStrategy_プロパティが正しい()
    {
        var strategy = new MarkdownReportStrategy();

        Assert.Equal("text/markdown", strategy.ContentType);
        Assert.Equal("md", strategy.FileExtension);
    }

    [Fact]
    public void MdStrategy_Markdown構造を生成する()
    {
        var strategy = new MarkdownReportStrategy();
        var context = CreateTestContext();

        var result = strategy.Generate(context);

        Assert.Contains("# Perfmon Analyzer レポート", result);
        Assert.Contains("## サマリ", result);
        Assert.Contains("|---|", result);
    }

    [Fact]
    public void MdStrategy_メタ情報が箇条書きで出力される()
    {
        var strategy = new MarkdownReportStrategy();
        var context = CreateTestContext();

        var result = strategy.Generate(context);

        Assert.Contains("- **生成日時:**", result);
        Assert.Contains("- **分析期間:**", result);
        Assert.Contains("- **閾値:**", result);
    }

    [Fact]
    public void MdStrategy_警告カウンタが閾値超過セクションに表示される()
    {
        var strategy = new MarkdownReportStrategy();
        var context = CreateTestContext(withWarning: true);

        var result = strategy.Generate(context);

        Assert.Contains("## 閾値超過カウンタ一覧", result);
        Assert.Contains("80.50", result);
    }

    [Fact]
    public void MdStrategy_不正なBase64でフォールバックメッセージを出す()
    {
        var strategy = new MarkdownReportStrategy();
        var context = CreateTestContext(chartImage: "javascript:alert('xss')");

        var result = strategy.Generate(context);

        Assert.DoesNotContain("![", result);
        Assert.Contains("グラフ画像の形式が不正です", result);
    }

    #endregion
}
