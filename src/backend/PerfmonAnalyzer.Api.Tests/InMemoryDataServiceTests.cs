using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Tests;

public class InMemoryDataServiceTests
{
    private readonly InMemoryDataService _service = new();

    #region ヘルパーメソッド

    private static List<CounterInfo> CreateSampleCounters()
    {
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        return
        [
            new CounterInfo
            {
                MachineName = "SERVER",
                Category = "Memory",
                InstanceName = "",
                CounterName = "Available MBytes",
                DisplayName = "\\\\SERVER\\Memory\\Available MBytes",
                DataPoints =
                [
                    new DataPoint { Timestamp = baseTime.AddMinutes(0), Value = 1024 },
                    new DataPoint { Timestamp = baseTime.AddMinutes(1), Value = 1020 },
                    new DataPoint { Timestamp = baseTime.AddMinutes(2), Value = 1016 },
                    new DataPoint { Timestamp = baseTime.AddMinutes(3), Value = 1012 },
                    new DataPoint { Timestamp = baseTime.AddMinutes(4), Value = 1008 },
                ]
            },
            new CounterInfo
            {
                MachineName = "SERVER",
                Category = "Processor",
                InstanceName = "_Total",
                CounterName = "% Processor Time",
                DisplayName = "\\\\SERVER\\Processor(_Total)\\% Processor Time",
                DataPoints =
                [
                    new DataPoint { Timestamp = baseTime.AddMinutes(0), Value = 10 },
                    new DataPoint { Timestamp = baseTime.AddMinutes(1), Value = 20 },
                    new DataPoint { Timestamp = baseTime.AddMinutes(2), Value = 30 },
                    new DataPoint { Timestamp = baseTime.AddMinutes(3), Value = 40 },
                    new DataPoint { Timestamp = baseTime.AddMinutes(4), Value = 50 },
                ]
            }
        ];
    }

    #endregion

    [Fact]
    public void CreateSession_ReturnsUniqueSessionId()
    {
        // Arrange
        var counters = CreateSampleCounters();

        // Act
        var sessionId1 = _service.CreateSession(counters);
        var sessionId2 = _service.CreateSession(counters);

        // Assert
        Assert.NotNull(sessionId1);
        Assert.NotNull(sessionId2);
        Assert.NotEmpty(sessionId1);
        Assert.NotEmpty(sessionId2);
        Assert.NotEqual(sessionId1, sessionId2);
    }

    [Fact]
    public void GetCounters_ReturnsStoredData()
    {
        // Arrange
        var counters = CreateSampleCounters();
        var sessionId = _service.CreateSession(counters);

        // Act
        var result = _service.GetCounters(sessionId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Available MBytes", result[0].CounterName);
        Assert.Equal("% Processor Time", result[1].CounterName);
        Assert.Equal(5, result[0].DataPoints.Length);
        Assert.Equal(5, result[1].DataPoints.Length);
    }

    [Fact]
    public void GetCounters_WithTimeRange_FiltersData()
    {
        // Arrange
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var counters = CreateSampleCounters();
        var sessionId = _service.CreateSession(counters);

        // Act - 1分目から3分目までのデータを取得（0分目と4分目を除外）
        var result = _service.GetCounters(sessionId, baseTime.AddMinutes(1), baseTime.AddMinutes(3));

        // Assert
        Assert.Equal(2, result.Count);
        // 各カウンタのデータポイントが3つ（1, 2, 3分目）に絞られていること
        Assert.Equal(3, result[0].DataPoints.Length);
        Assert.Equal(3, result[1].DataPoints.Length);

        // フィルタされたデータの値が正しいこと
        Assert.Equal(1020, result[0].DataPoints[0].Value);
        Assert.Equal(1016, result[0].DataPoints[1].Value);
        Assert.Equal(1012, result[0].DataPoints[2].Value);
    }

    [Fact]
    public void GetCounters_InvalidSessionId_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _service.GetCounters("nonexistent"));
    }

    [Fact]
    public void GetCounters_WithTimeRange_InvalidSessionId_ThrowsException()
    {
        // Act & Assert
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        Assert.Throws<KeyNotFoundException>(() =>
            _service.GetCounters("nonexistent", baseTime, baseTime.AddMinutes(5)));
    }

    [Fact]
    public void SessionExists_ReturnsTrueForExistingSession()
    {
        // Arrange
        var sessionId = _service.CreateSession(CreateSampleCounters());

        // Act & Assert
        Assert.True(_service.SessionExists(sessionId));
    }

    [Fact]
    public void SessionExists_ReturnsFalseForNonExistingSession()
    {
        // Act & Assert
        Assert.False(_service.SessionExists("nonexistent"));
    }

    [Fact]
    public void CleanupExpiredSessions_RemovesOldSessions()
    {
        // Arrange
        var counters = CreateSampleCounters();
        var sessionId = _service.CreateSession(counters);

        // セッションが存在することを確認
        Assert.True(_service.SessionExists(sessionId));

        // Act - 有効期間0秒（即座に期限切れ）でクリーンアップ
        _service.CleanupExpiredSessions(TimeSpan.Zero);

        // Assert
        Assert.False(_service.SessionExists(sessionId));
    }

    [Fact]
    public void CleanupExpiredSessions_KeepsRecentSessions()
    {
        // Arrange
        var sessionId = _service.CreateSession(CreateSampleCounters());

        // Act - 有効期間1時間でクリーンアップ（まだ期限内）
        _service.CleanupExpiredSessions(TimeSpan.FromHours(1));

        // Assert - セッションはまだ存在する
        Assert.True(_service.SessionExists(sessionId));
    }

    [Fact]
    public void CreateSession_MultipleSessionsManaged()
    {
        // Arrange
        var counters1 = new List<CounterInfo>
        {
            new()
            {
                MachineName = "SERVER1",
                Category = "Memory",
                CounterName = "Counter1",
                DisplayName = "Counter1",
                DataPoints = [new DataPoint { Timestamp = DateTime.UtcNow, Value = 100 }]
            }
        };

        var counters2 = new List<CounterInfo>
        {
            new()
            {
                MachineName = "SERVER2",
                Category = "Processor",
                CounterName = "Counter2",
                DisplayName = "Counter2",
                DataPoints = [new DataPoint { Timestamp = DateTime.UtcNow, Value = 200 }]
            }
        };

        // Act
        var sessionId1 = _service.CreateSession(counters1);
        var sessionId2 = _service.CreateSession(counters2);

        // Assert - 各セッションが独立してデータを管理していること
        var result1 = _service.GetCounters(sessionId1);
        var result2 = _service.GetCounters(sessionId2);

        Assert.Single(result1);
        Assert.Single(result2);
        Assert.Equal("Counter1", result1[0].CounterName);
        Assert.Equal("Counter2", result2[0].CounterName);
    }
}
