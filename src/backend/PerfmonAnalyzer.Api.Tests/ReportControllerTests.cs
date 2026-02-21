using Microsoft.AspNetCore.Mvc;
using Moq;
using PerfmonAnalyzer.Api.Controllers;
using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Tests;

public class ReportControllerTests
{
    private readonly Mock<IReportGenerator> _mockReportGenerator;
    private readonly Mock<ISlopeAnalyzer> _mockSlopeAnalyzer;
    private readonly Mock<IDataService> _mockDataService;
    private readonly ReportController _controller;

    public ReportControllerTests()
    {
        _mockReportGenerator = new Mock<IReportGenerator>();
        _mockSlopeAnalyzer = new Mock<ISlopeAnalyzer>();
        _mockDataService = new Mock<IDataService>();
        _controller = new ReportController(
            _mockReportGenerator.Object,
            _mockSlopeAnalyzer.Object,
            _mockDataService.Object);
    }

    [Fact]
    public void GenerateReport_EmptySessionId_ReturnsBadRequest()
    {
        // Arrange
        var request = new ReportRequest
        {
            SessionId = "",
            StartTime = new DateTime(2026, 1, 1),
            EndTime = new DateTime(2026, 1, 2),
        };

        // Act
        var result = _controller.GenerateReport(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GenerateReport_InvalidTimeRange_ReturnsBadRequest()
    {
        // Arrange
        var request = new ReportRequest
        {
            SessionId = "test-session",
            StartTime = new DateTime(2026, 1, 2),
            EndTime = new DateTime(2026, 1, 1),
        };

        // Act
        var result = _controller.GenerateReport(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GenerateReport_NonExistentSession_ReturnsNotFound()
    {
        // Arrange
        var request = new ReportRequest
        {
            SessionId = "nonexistent",
            StartTime = new DateTime(2026, 1, 1),
            EndTime = new DateTime(2026, 1, 2),
        };
        _mockDataService
            .Setup(s => s.GetCounters("nonexistent", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Throws(new KeyNotFoundException("Session not found"));

        // Act
        var result = _controller.GenerateReport(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void GenerateReport_ValidRequest_ReturnsFileResult()
    {
        // Arrange
        var sessionId = "test-session";
        var startTime = new DateTime(2026, 1, 1);
        var endTime = new DateTime(2026, 1, 2);

        var request = new ReportRequest
        {
            SessionId = sessionId,
            StartTime = startTime,
            EndTime = endTime,
            ThresholdKBPer10Min = 50,
            ChartImageBase64 = "data:image/png;base64,abc123",
            Format = "html",
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

        var slopeResults = new List<SlopeResult>
        {
            new()
            {
                CounterName = "\\\\SERVER\\Memory\\Available MBytes",
                SlopeKBPer10Min = 10.0,
                IsWarning = false,
                RSquared = 0.99
            }
        };

        var reportResponse = new ReportResponse
        {
            Content = "<html>test</html>",
            FileName = "report.html",
            ContentType = "text/html",
        };

        _mockDataService.Setup(s => s.GetCounters(sessionId, startTime, endTime)).Returns(counters);
        _mockSlopeAnalyzer
            .Setup(a => a.Calculate(counters, startTime, endTime, 50))
            .Returns(slopeResults);
        _mockReportGenerator
            .Setup(g => g.GenerateReport(
                counters, slopeResults, startTime, endTime, 50, "data:image/png;base64,abc123", "html"))
            .Returns(reportResponse);

        // Act
        var result = _controller.GenerateReport(request);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/html", fileResult.ContentType);
        Assert.Equal("report.html", fileResult.FileDownloadName);
    }

    [Fact]
    public void GenerateReport_MarkdownFormat_ReturnsMarkdownFile()
    {
        // Arrange
        var sessionId = "test-session";
        var startTime = new DateTime(2026, 1, 1);
        var endTime = new DateTime(2026, 1, 2);

        var request = new ReportRequest
        {
            SessionId = sessionId,
            StartTime = startTime,
            EndTime = endTime,
            Format = "md",
        };

        var counters = new List<CounterInfo>();
        var slopeResults = new List<SlopeResult>();

        var reportResponse = new ReportResponse
        {
            Content = "# Report",
            FileName = "report.md",
            ContentType = "text/markdown",
        };

        _mockDataService.Setup(s => s.GetCounters(sessionId, startTime, endTime)).Returns(counters);
        _mockSlopeAnalyzer
            .Setup(a => a.Calculate(counters, startTime, endTime, 50))
            .Returns(slopeResults);
        _mockReportGenerator
            .Setup(g => g.GenerateReport(
                counters, slopeResults, startTime, endTime, 50, "", "md"))
            .Returns(reportResponse);

        // Act
        var result = _controller.GenerateReport(request);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/markdown", fileResult.ContentType);
        Assert.Equal("report.md", fileResult.FileDownloadName);
    }
}
