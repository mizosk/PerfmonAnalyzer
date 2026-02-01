using Microsoft.AspNetCore.Mvc;

namespace PerfmonAnalyzer.Api.Controllers;

/// <summary>
/// ヘルスチェック用コントローラー
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// APIの稼働状態を確認します
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
