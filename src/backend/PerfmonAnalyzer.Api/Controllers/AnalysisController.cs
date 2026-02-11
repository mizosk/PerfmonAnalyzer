using Microsoft.AspNetCore.Mvc;
using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Controllers;

/// <summary>
/// データ分析を行うコントローラー
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly ISlopeAnalyzer _slopeAnalyzer;
    private readonly IDataService _dataService;

    public AnalysisController(ISlopeAnalyzer slopeAnalyzer, IDataService dataService)
    {
        _slopeAnalyzer = slopeAnalyzer;
        _dataService = dataService;
    }

    /// <summary>
    /// 指定されたカウンタデータに対して傾き分析を行う
    /// </summary>
    /// <param name="request">傾き分析リクエスト</param>
    /// <returns>傾き分析結果</returns>
    [HttpPost("slope")]
    public ActionResult<SlopeResponse> CalculateSlope([FromBody] SlopeRequest request)
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
            var counters = _dataService.GetCounters(request.SessionId, request.StartTime, request.EndTime);

            var results = _slopeAnalyzer.Calculate(
                counters,
                request.StartTime,
                request.EndTime,
                request.ThresholdKBPer10Min);

            return Ok(new SlopeResponse { Results = results });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Session {request.SessionId} が見つかりません。" });
        }
    }
}
