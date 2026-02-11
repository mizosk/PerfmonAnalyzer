using Microsoft.AspNetCore.Mvc;
using Moq;
using PerfmonAnalyzer.Api.Controllers;
using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Tests;

public class AnalysisControllerTests
{
    private readonly Mock<ISlopeAnalyzer> _mockSlopeAnalyzer;
    private readonly Mock<IDataService> _mockDataService;
    private readonly AnalysisController _controller;

    public AnalysisControllerTests()
    {
        _mockSlopeAnalyzer = new Mock<ISlopeAnalyzer>();
        _mockDataService = new Mock<IDataService>();
        _controller = new AnalysisController(_mockSlopeAnalyzer.Object, _mockDataService.Object);
    }

    [Fact]
    public void CalculateSlope_EmptySessionId_ReturnsBadRequest()
    {
        // Arrange
        var request = new SlopeRequest
        {
            SessionId = "",
            StartTime = new DateTime(2026, 1, 1),
            EndTime = new DateTime(2026, 1, 2),
        };

        // Act
        var result = _controller.CalculateSlope(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void CalculateSlope_NonExistentSession_ReturnsNotFound()
    {
        // Arrange
        var request = new SlopeRequest
        {
            SessionId = "nonexistent",
            StartTime = new DateTime(2026, 1, 1),
            EndTime = new DateTime(2026, 1, 2),
        };
        _mockDataService.Setup(s => s.SessionExists("nonexistent")).Returns(false);

        // Act
        var result = _controller.CalculateSlope(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public void CalculateSlope_ValidRequest_ReturnsResults()
    {
        // Arrange
        var sessionId = "test-session";
        var startTime = new DateTime(2026, 1, 1);
        var endTime = new DateTime(2026, 1, 2);

        var request = new SlopeRequest
        {
            SessionId = sessionId,
            StartTime = startTime,
            EndTime = endTime,
            ThresholdKBPer10Min = 50,
        };

        var counters = new List<CounterInfo>
        {
            new()
            {
                MachineName = "SERVER",
                Category = "Memory",
                CounterName = "Available MBytes",
                DisplayName = "\\\\SERVER\\Memory\\Available MBytes",
                DataPoints = [new DataPoint { Timestamp = startTime, Value = 100 }]
            }
        };

        var expectedResults = new List<SlopeResult>
        {
            new()
            {
                CounterName = "\\\\SERVER\\Memory\\Available MBytes",
                SlopeKBPer10Min = 10.0,
                IsWarning = false,
                RSquared = 0.99
            }
        };

        _mockDataService.Setup(s => s.SessionExists(sessionId)).Returns(true);
        _mockDataService.Setup(s => s.GetCounters(sessionId, startTime, endTime)).Returns(counters);
        _mockSlopeAnalyzer
            .Setup(a => a.Calculate(counters, startTime, endTime, 50))
            .Returns(expectedResults);

        // Act
        var result = _controller.CalculateSlope(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SlopeResponse>(okResult.Value);
        Assert.Single(response.Results);
        Assert.Equal(10.0, response.Results[0].SlopeKBPer10Min);
    }

    [Fact]
    public void CalculateSlope_InvalidTimeRange_ReturnsBadRequest()
    {
        // Arrange
        var request = new SlopeRequest
        {
            SessionId = "test-session",
            StartTime = new DateTime(2026, 1, 2),
            EndTime = new DateTime(2026, 1, 1), // EndTime < StartTime
        };

        // Act
        var result = _controller.CalculateSlope(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
