using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Tests;

public class ReportGeneratorTests
{
    private readonly IReportGenerator _generator = new ReportGenerator();

    #region ヘルパーメソッド

    /// <summary>
    /// テスト用カウンタデータを作成するヘルパー
    /// </summary>
    private static List<CounterInfo> CreateTestCounters(int count = 3)
    {
        var counters = new List<CounterInfo>();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);

        for (int i = 0; i < count; i++)
        {
            counters.Add(new CounterInfo
            {
                MachineName = "SERVER",
                Category = "Process",
                InstanceName = $"Process{i}",
                CounterName = "Working Set - Private",
                DisplayName = $"\\\\SERVER\\Process(Process{i})\\Working Set - Private",
                DataPoints = new[]
                {
                    new DataPoint { Timestamp = baseTime, Value = 1000 + i * 100 },
                    new DataPoint { Timestamp = baseTime.AddMinutes(10), Value = 1100 + i * 100 },
                    new DataPoint { Timestamp = baseTime.AddMinutes(20), Value = 1200 + i * 100 },
                }
            });
        }
        return counters;
    }

    /// <summary>
    /// テスト用傾き分析結果を作成するヘルパー
    /// </summary>
    private static List<SlopeResult> CreateTestSlopeResults(int count = 3, bool withWarning = true)
    {
        var results = new List<SlopeResult>();
        for (int i = 0; i < count; i++)
        {
            results.Add(new SlopeResult
            {
                CounterName = $"\\\\SERVER\\Process(Process{i})\\Working Set - Private",
                SlopeKBPer10Min = withWarning && i == 0 ? 80.5 : 10.0 + i,
                IsWarning = withWarning && i == 0,
                RSquared = 0.95 + i * 0.01
            });
        }
        return results;
    }

    private static readonly DateTime TestStartTime = new(2026, 1, 1, 0, 0, 0);
    private static readonly DateTime TestEndTime = new(2026, 1, 1, 0, 20, 0);
    private const double TestThreshold = 50.0;
    private const string TestChartImage = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUg==";

    #endregion

    #region HTML形式のテスト

    [Fact]
    public void GenerateReport_HtmlFormat_ReturnsHtmlContent()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "html");

        // Assert
        Assert.Equal("text/html", result.ContentType);
        Assert.EndsWith(".html", result.FileName);
        Assert.Contains("<html", result.Content);
        Assert.Contains("</html>", result.Content);
    }

    [Fact]
    public void GenerateReport_HtmlFormat_ContainsReportTitle()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "html");

        // Assert
        Assert.Contains("Perfmon Analyzer レポート", result.Content);
    }

    [Fact]
    public void GenerateReport_HtmlFormat_ContainsAnalysisPeriod()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "html");

        // Assert
        Assert.Contains("2026-01-01", result.Content);
    }

    [Fact]
    public void GenerateReport_HtmlFormat_ContainsThresholdInfo()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "html");

        // Assert
        Assert.Contains("50", result.Content);
        Assert.Contains("KB/10min", result.Content);
    }

    [Fact]
    public void GenerateReport_HtmlFormat_ContainsSummarySection()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults(withWarning: true);

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "html");

        // Assert
        // 総カウンタ数
        Assert.Contains("3", result.Content);
        // 警告カウンタ数（1件が警告）
        Assert.Contains("1", result.Content);
    }

    [Fact]
    public void GenerateReport_HtmlFormat_ContainsChartImage()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "html");

        // Assert
        Assert.Contains("<img", result.Content);
        Assert.Contains("iVBORw0KGgoAAAANSUhEUg==", result.Content);
    }

    [Fact]
    public void GenerateReport_HtmlFormat_ContainsCounterDetails()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "html");

        // Assert
        Assert.Contains("Process0", result.Content);
        Assert.Contains("Process1", result.Content);
        Assert.Contains("Process2", result.Content);
        Assert.Contains("Working Set - Private", result.Content);
    }

    [Fact]
    public void GenerateReport_HtmlFormat_HighlightsWarningRows()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults(withWarning: true);

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "html");

        // Assert
        // 警告行は赤色ハイライトを含む
        Assert.Contains("warning", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Markdown形式のテスト

    [Fact]
    public void GenerateReport_MdFormat_ReturnsMarkdownContent()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "md");

        // Assert
        Assert.Equal("text/markdown", result.ContentType);
        Assert.EndsWith(".md", result.FileName);
    }

    [Fact]
    public void GenerateReport_MdFormat_ContainsMarkdownHeadings()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "md");

        // Assert
        Assert.Contains("# Perfmon Analyzer レポート", result.Content);
        Assert.Contains("## ", result.Content);
    }

    [Fact]
    public void GenerateReport_MdFormat_ContainsMarkdownTable()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "md");

        // Assert
        // Markdown テーブルのセパレータ行
        Assert.Contains("|---|", result.Content);
        Assert.Contains("Process0", result.Content);
    }

    [Fact]
    public void GenerateReport_MdFormat_ContainsChartImage()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "md");

        // Assert
        Assert.Contains("![", result.Content);
        Assert.Contains("iVBORw0KGgoAAAANSUhEUg==", result.Content);
    }

    [Fact]
    public void GenerateReport_MdFormat_ContainsWarningSection()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults(withWarning: true);

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "md");

        // Assert
        // 警告セクションが存在すること
        Assert.Contains("閾値超過", result.Content);
        Assert.Contains("80.5", result.Content);
    }

    #endregion

    #region 共通テスト

    [Fact]
    public void GenerateReport_EmptyCounters_DoesNotThrow()
    {
        // Arrange
        var counters = new List<CounterInfo>();
        var slopeResults = new List<SlopeResult>();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "html");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.Contains("0", result.Content); // 総カウンタ数: 0
    }

    [Fact]
    public void GenerateReport_EmptyChartImage_DoesNotThrow()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, "", "html");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public void GenerateReport_NoWarnings_WarningCountIsZero()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults(withWarning: false);

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "html");

        // Assert
        Assert.NotNull(result);
        // 全カウンタが isWarning = false なので警告数 0
        Assert.Contains("警告カウンタ数: 0", result.Content);
    }

    [Fact]
    public void GenerateReport_InvalidFormat_DefaultsToHtml()
    {
        // Arrange
        var counters = CreateTestCounters();
        var slopeResults = CreateTestSlopeResults();

        // Act
        var result = _generator.GenerateReport(
            counters, slopeResults, TestStartTime, TestEndTime, TestThreshold, TestChartImage, "unknown");

        // Assert
        Assert.Equal("text/html", result.ContentType);
        Assert.Contains("<html", result.Content);
    }

    #endregion
}
