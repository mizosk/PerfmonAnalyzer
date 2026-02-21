using System.Text;
using Microsoft.AspNetCore.Mvc;
using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Controllers;

/// <summary>
/// レポート生成を行うコントローラー
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IReportGenerator _reportGenerator;
    private readonly ISlopeAnalyzer _slopeAnalyzer;
    private readonly IDataService _dataService;

    public ReportController(
        IReportGenerator reportGenerator,
        ISlopeAnalyzer slopeAnalyzer,
        IDataService dataService)
    {
        _reportGenerator = reportGenerator;
        _slopeAnalyzer = slopeAnalyzer;
        _dataService = dataService;
    }

    /// <summary>
    /// レポートを生成してファイルとして返却する
    /// </summary>
    /// <param name="request">レポート生成リクエスト</param>
    /// <returns>生成されたレポートファイル</returns>
    [HttpPost("generate")]
    public IActionResult GenerateReport([FromBody] ReportRequest request)
    {
        if (string.IsNullOrEmpty(request.SessionId))
        {
            return BadRequest(new { error = "SessionId は必須です。" });
        }

        if (request.StartTime >= request.EndTime)
        {
            return BadRequest(new { error = "StartTime は EndTime より前でなければなりません。" });
        }

        try
        {
            // 1. カウンターデータを取得
            var counters = _dataService.GetCounters(request.SessionId, request.StartTime, request.EndTime);

            // 2. 傾き分析を実行
            var slopeResults = _slopeAnalyzer.Calculate(
                counters,
                request.StartTime,
                request.EndTime,
                request.ThresholdKBPer10Min);

            // 3. レポート生成
            var report = _reportGenerator.GenerateReport(
                counters,
                slopeResults,
                request.StartTime,
                request.EndTime,
                request.ThresholdKBPer10Min,
                request.ChartImageBase64,
                request.Format);

            // 4. ファイルとして返却
            var bytes = Encoding.UTF8.GetBytes(report.Content);
            return File(bytes, report.ContentType, report.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Session {request.SessionId} が見つかりません。" });
        }
    }
}
