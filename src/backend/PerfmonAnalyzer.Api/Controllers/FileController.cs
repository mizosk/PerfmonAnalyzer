using Microsoft.AspNetCore.Mvc;
using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Controllers;

/// <summary>
/// CSV ファイルのアップロードを処理するコントローラー
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

    private readonly ICsvImporter _csvImporter;

    public FileController(ICsvImporter csvImporter)
    {
        _csvImporter = csvImporter;
    }

    /// <summary>
    /// Perfmon CSV ファイルをアップロードしてパースする
    /// </summary>
    /// <param name="file">アップロードする CSV ファイル</param>
    /// <returns>パース結果（セッションIDとカウンタ一覧）</returns>
    [HttpPost("upload")]
    public async Task<ActionResult<UploadResult>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "ファイルが空です。" });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { error = "ファイルサイズが上限（50MB）を超えています。" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var counters = await _csvImporter.ImportAsync(stream, HttpContext.RequestAborted);

            var result = new UploadResult
            {
                SessionId = Guid.NewGuid().ToString(),
                Counters = counters,
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"CSV の処理中にエラーが発生しました: {ex.Message}" });
        }
    }
}
