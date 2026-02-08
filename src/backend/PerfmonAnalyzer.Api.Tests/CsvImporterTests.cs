using System.Text;
using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Tests;

public class CsvImporterTests
{
    /// <summary>
    /// テスト用サンプル CSV（UTF-8 BOM付き）
    /// </summary>
    private static readonly string SampleCsv = string.Join("\r\n",
        "\"(PDH-CSV 4.0) (Tokyo Standard Time)(540)\",\"\\\\SERVER\\Processor(_Total)\\% Processor Time\",\"\\\\SERVER\\Memory\\Available MBytes\"",
        "\"02/08/2026 00:00:01.000\",\"25.5\",\"1024\"",
        "\"02/08/2026 00:00:02.000\",\"30.2\",\"\"",
        "\"02/08/2026 00:00:03.000\",\" \",\"512.8\""
    );

    private static Stream CreateUtf8Stream(string content)
    {
        // UTF-8 BOM 付きストリームを生成
        var preamble = Encoding.UTF8.GetPreamble();
        var bytes = Encoding.UTF8.GetBytes(content);
        var combined = new byte[preamble.Length + bytes.Length];
        Buffer.BlockCopy(preamble, 0, combined, 0, preamble.Length);
        Buffer.BlockCopy(bytes, 0, combined, preamble.Length, bytes.Length);
        return new MemoryStream(combined);
    }

    private static Stream CreateShiftJisStream(string content)
    {
        // BOM なし Shift-JIS ストリーム
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding("shift_jis");
        var bytes = encoding.GetBytes(content);
        return new MemoryStream(bytes);
    }

    [Fact]
    public async Task ImportAsync_ValidCsv_ExtractsCorrectCounterCount()
    {
        // Arrange
        var importer = new CsvImporter();
        using var stream = CreateUtf8Stream(SampleCsv);

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert - ヘッダの最初の列はタイムスタンプなのでカウンタは2つ
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ImportAsync_ValidCsv_ExtractsCounterInfoCorrectly()
    {
        // Arrange
        var importer = new CsvImporter();
        using var stream = CreateUtf8Stream(SampleCsv);

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert - 1番目のカウンタ: Processor(_Total)\% Processor Time
        var counter1 = result[0];
        Assert.Equal("SERVER", counter1.MachineName);
        Assert.Equal("Processor", counter1.Category);
        Assert.Equal("_Total", counter1.InstanceName);
        Assert.Equal("% Processor Time", counter1.CounterName);

        // Assert - 2番目のカウンタ: Memory\Available MBytes（インスタンスなし）
        var counter2 = result[1];
        Assert.Equal("SERVER", counter2.MachineName);
        Assert.Equal("Memory", counter2.Category);
        Assert.Equal("", counter2.InstanceName);
        Assert.Equal("Available MBytes", counter2.CounterName);
    }

    [Fact]
    public async Task ImportAsync_ValidCsv_ParsesTimestampsCorrectly()
    {
        // Arrange
        var importer = new CsvImporter();
        using var stream = CreateUtf8Stream(SampleCsv);

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert - 各カウンタに3つのデータポイントがあること
        Assert.Equal(3, result[0].DataPoints.Length);
        Assert.Equal(3, result[1].DataPoints.Length);

        // タイムスタンプが正しくパースされること
        Assert.Equal(new DateTime(2026, 2, 8, 0, 0, 1), result[0].DataPoints[0].Timestamp);
        Assert.Equal(new DateTime(2026, 2, 8, 0, 0, 2), result[0].DataPoints[1].Timestamp);
        Assert.Equal(new DateTime(2026, 2, 8, 0, 0, 3), result[0].DataPoints[2].Timestamp);
    }

    [Fact]
    public async Task ImportAsync_ValidCsv_ParsesValuesCorrectly()
    {
        // Arrange
        var importer = new CsvImporter();
        using var stream = CreateUtf8Stream(SampleCsv);

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert - 1番目のカウンタの値
        Assert.Equal(25.5, result[0].DataPoints[0].Value);
        Assert.Equal(30.2, result[0].DataPoints[1].Value);

        // Assert - 2番目のカウンタの値
        Assert.Equal(1024.0, result[1].DataPoints[0].Value);
        Assert.Equal(512.8, result[1].DataPoints[2].Value);
    }

    [Fact]
    public async Task ImportAsync_MissingValues_TreatedAsNaN()
    {
        // Arrange
        var importer = new CsvImporter();
        using var stream = CreateUtf8Stream(SampleCsv);

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert - 空セルは NaN になること
        // 2行目の Memory は空 ("")
        Assert.True(double.IsNaN(result[1].DataPoints[1].Value));

        // 3行目の Processor は空白 (" ")
        Assert.True(double.IsNaN(result[0].DataPoints[2].Value));
    }

    [Fact]
    public async Task ImportAsync_ShiftJisCsv_ParsedCorrectly()
    {
        // Arrange
        var importer = new CsvImporter();
        using var stream = CreateShiftJisStream(SampleCsv);

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert - Shift-JIS でもカウンタが正しくパースされること
        Assert.Equal(2, result.Count);
        Assert.Equal("SERVER", result[0].MachineName);
        Assert.Equal(3, result[0].DataPoints.Length);
    }

    [Fact]
    public async Task ImportAsync_DisplayName_ContainsOriginalHeader()
    {
        // Arrange
        var importer = new CsvImporter();
        using var stream = CreateUtf8Stream(SampleCsv);

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert - DisplayName にヘッダの生文字列が含まれること
        Assert.Contains("Processor", result[0].DisplayName);
        Assert.Contains("Memory", result[1].DisplayName);
    }
}
