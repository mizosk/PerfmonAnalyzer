using Microsoft.AspNetCore.Mvc;
using Moq;
using PerfmonAnalyzer.Api.Controllers;
using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Tests;

public class DataControllerTests
{
    private readonly Mock<IDataService> _mockDataService;
    private readonly DataController _controller;

    public DataControllerTests()
    {
        _mockDataService = new Mock<IDataService>();
        _controller = new DataController(_mockDataService.Object);
    }

    [Fact]
    public void GetData_ValidSessionId_ReturnsCounters()
    {
        // Arrange
        var sessionId = "test-session";
        var counters = new List<CounterInfo>
        {
            new()
            {
                MachineName = "SERVER",
                Category = "Memory",
                CounterName = "Available MBytes",
                DisplayName = "\\\\SERVER\\Memory\\Available MBytes",
                DataPoints = [new DataPoint { Timestamp = new DateTime(2026, 1, 1), Value = 100 }],
            },
        };
        _mockDataService.Setup(s => s.GetCounters(sessionId)).Returns(counters);

        // Act
        var result = _controller.GetData(sessionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DataResponse>(okResult.Value);
        Assert.Single(response.Counters);
        Assert.Equal("Available MBytes", response.Counters[0].CounterName);
    }

    [Fact]
    public void GetData_ValidSessionId_WithTimeRange_ReturnsFilteredData()
    {
        // Arrange
        var sessionId = "test-session";
        var startTime = new DateTime(2026, 1, 1);
        var endTime = new DateTime(2026, 1, 2);
        var counters = new List<CounterInfo>
        {
            new()
            {
                MachineName = "SERVER",
                Category = "Memory",
                CounterName = "Available MBytes",
                DisplayName = "\\\\SERVER\\Memory\\Available MBytes",
                DataPoints = [new DataPoint { Timestamp = new DateTime(2026, 1, 1, 12, 0, 0), Value = 200 }],
            },
        };
        _mockDataService.Setup(s => s.GetCounters(sessionId, startTime, endTime)).Returns(counters);

        // Act
        var result = _controller.GetData(sessionId, startTime, endTime);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DataResponse>(okResult.Value);
        Assert.Single(response.Counters);
        _mockDataService.Verify(s => s.GetCounters(sessionId, startTime, endTime), Times.Once);
    }

    [Fact]
    public void GetData_NonExistentSessionId_ReturnsNotFound()
    {
        // Arrange
        var sessionId = "nonexistent";
        _mockDataService
            .Setup(s => s.GetCounters(sessionId))
            .Throws(new KeyNotFoundException($"Session {sessionId} not found"));

        // Act
        var result = _controller.GetData(sessionId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public void GetData_OnlyStartTime_ReturnsBadRequest()
    {
        // Arrange
        var sessionId = "test-session";
        var startTime = new DateTime(2026, 1, 1);

        // Act
        var result = _controller.GetData(sessionId, startTime: startTime, endTime: null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void GetData_OnlyEndTime_ReturnsBadRequest()
    {
        // Arrange
        var sessionId = "test-session";
        var endTime = new DateTime(2026, 1, 2);

        // Act
        var result = _controller.GetData(sessionId, startTime: null, endTime: endTime);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
