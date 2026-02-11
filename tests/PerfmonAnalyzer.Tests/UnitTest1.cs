using PerfmonAnalyzer.Models;

namespace PerfmonAnalyzer.Tests;

public class DataPointTests
{
    [Fact]
    public void DataPoint_ShouldStoreTimestampAndValue()
    {
        // Arrange
        var timestamp = new DateTime(2026, 1, 15, 10, 0, 0);
        var value = 123.45;

        // Act
        var dataPoint = new DataPoint
        {
            Timestamp = timestamp,
            Value = value
        };

        // Assert
        Assert.Equal(timestamp, dataPoint.Timestamp);
        Assert.Equal(value, dataPoint.Value);
    }

    [Fact]
    public void CounterInfo_ShouldInitializeWithEmptyData()
    {
        // Act
        var counterInfo = new CounterInfo();

        // Assert
        Assert.NotNull(counterInfo.Data);
        Assert.Empty(counterInfo.Data);
        Assert.Equal(string.Empty, counterInfo.Name);
    }

    [Fact]
    public void SlopeResult_ShouldStoreAnalysisResults()
    {
        // Arrange & Act
        var result = new SlopeResult
        {
            CounterName = "Process(app)\\Private Bytes",
            SlopeKBPer10Min = 50.5,
            IsWarning = true,
            RSquared = 0.95
        };

        // Assert
        Assert.Equal("Process(app)\\Private Bytes", result.CounterName);
        Assert.Equal(50.5, result.SlopeKBPer10Min);
        Assert.True(result.IsWarning);
        Assert.Equal(0.95, result.RSquared);
    }
}
