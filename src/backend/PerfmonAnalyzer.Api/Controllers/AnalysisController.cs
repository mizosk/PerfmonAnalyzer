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

    public AnalysisController(ISlopeAnalyzer slopeAnalyzer)
    {
        _slopeAnalyzer = slopeAnalyzer;
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

        // TODO: SessionId からカウンタデータを取得する仕組みが必要
        // 現時点ではセッション管理は未実装のため、空のレスポンスを返す
        var response = new SlopeResponse
        {
            Results = []
        };

        return Ok(response);
    }
}
