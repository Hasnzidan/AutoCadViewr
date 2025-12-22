using DWGViewerAPI.Infrastructure;
using DWGViewerAPI.Models;
using DWGViewerAPI.Models.Requests;
using DWGViewerAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DWGViewerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DwgController : ControllerBase
    {
        private readonly IDwgParserService _parserService;
        private readonly FileDownloader _fileDownloader;
        private readonly ILogger<DwgController> _logger;

        public DwgController(
            IDwgParserService parserService,
            FileDownloader fileDownloader,
            ILogger<DwgController> logger
        )
        {
            _parserService = parserService;
            _fileDownloader = fileDownloader;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDwg(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            if (!file.FileName.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "File must be a .dwg file" });

            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".dwg");
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var result = _parserService.ParseDwgFile(tempPath);
                System.IO.File.Delete(tempPath);

                _logger.LogInformation(
                    $"Successfully parsed {result.Entities.Count} entities from {file.FileName}"
                );
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DWG file");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("parse")]
        public IActionResult ParseLocalFile([FromQuery] string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                return BadRequest(new { error = "Invalid file path" });

            try
            {
                var result = _parserService.ParseDwgFile(filePath);
                _logger.LogInformation(
                    $"Successfully parsed {result.Entities.Count} entities from {filePath}"
                );
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DWG file");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("parse-from-url")]
        public async Task<IActionResult> ParseFromUrl(
            [FromBody] DWGViewerAPI.Models.Requests.UrlRequest request
        )
        {
            if (string.IsNullOrEmpty(request.Url))
                return BadRequest(new { error = "URL is required" });

            try
            {
                var tempPath = await _fileDownloader.DownloadFileAsync(request.Url);
                var result = _parserService.ParseDwgFile(tempPath);
                System.IO.File.Delete(tempPath);

                _logger.LogInformation(
                    $"Successfully parsed {result.Entities.Count} entities from URL"
                );
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DWG from URL");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
