using Microsoft.AspNetCore.Mvc;
using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Controllers;

/// <summary>
/// セッションデータの取得を行うコントローラー
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly IDataService _dataService;

    public DataController(IDataService dataService)
    {
        _dataService = dataService;
    }

    /// <summary>
    /// セッションIDに紐づくカウンターデータを取得する
    /// </summary>
    /// <param name="sessionId">セッションID</param>
    /// <param name="startTime">取得開始時刻（省略可）</param>
    /// <param name="endTime">取得終了時刻（省略可）</param>
    /// <returns>カウンターデータ</returns>
    [HttpGet("{sessionId}")]
    public ActionResult<DataResponse> GetData(
        string sessionId,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        if (!_dataService.SessionExists(sessionId))
        {
            return NotFound(new { error = $"Session {sessionId} が見つかりません。" });
        }

        List<CounterInfo> counters;
        if (startTime.HasValue && endTime.HasValue)
        {
            counters = _dataService.GetCounters(sessionId, startTime.Value, endTime.Value);
        }
        else
        {
            counters = _dataService.GetCounters(sessionId);
        }

        var response = new DataResponse
        {
            Counters = counters,
        };

        return Ok(response);
    }
}
