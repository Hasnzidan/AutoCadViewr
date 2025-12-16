using Microsoft.AspNetCore.Mvc;
using DWGViewerAPI.Services;

namespace DWGViewerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DwgController : ControllerBase
    {
        private readonly DwgParserService _parserService;
        private readonly ILogger<DwgController> _logger;

        public DwgController(ILogger<DwgController> logger)
        {
            _logger = logger;
            _parserService = new DwgParserService();
        }

        /// <summary>
        /// رفع ملف DWG وتحويله إلى JSON
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDwg(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (!file.FileName.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "File must be a .dwg file" });
            }

            try
            {
                // حفظ الملف مؤقتاً
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".dwg");
                
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // قراءة وتحليل الملف
                var entities = _parserService.ParseDwgFile(tempPath);

                // حذف الملف المؤقت
                System.IO.File.Delete(tempPath);

                _logger.LogInformation($"Successfully parsed {entities.Count} entities from {file.FileName}");

                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DWG file");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// قراءة ملف DWG من المسار المحلي (للتجربة)
        /// </summary>
        [HttpGet("parse")]
        public IActionResult ParseLocalFile([FromQuery] string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return BadRequest(new { error = "Invalid file path" });
            }

            try
            {
                var entities = _parserService.ParseDwgFile(filePath);
                _logger.LogInformation($"Successfully parsed {entities.Count} entities from {filePath}");
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DWG file");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
