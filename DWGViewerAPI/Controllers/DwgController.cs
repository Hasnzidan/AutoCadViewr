using Microsoft.AspNetCore.Mvc;
using DWGViewerAPI.Services;
using DWGViewerAPI.Models;


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

        /// رفع ملف DWG وتحويله إلى JSON
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

        /// قراءة ملف DWG من المسار المحلي (للتجربة)
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

        //تحليل ملف DWG من رابط مباشر
       [HttpPost("parse-from-url")]
        public async Task<IActionResult> ParseFromUrl([FromBody] UrlRequest request)
        {
            if (string.IsNullOrEmpty(request.Url))
            {
                return BadRequest(new { error = "URL is required" });
            }

            try
            {
                // تحميل الملف من الرابط
                var tempPath = await DownloadFileFromUrl(request.Url);

                // قراءة وتحليل الملف
                var entities = _parserService.ParseDwgFile(tempPath);

                // حذف الملف المؤقت
                System.IO.File.Delete(tempPath);

                _logger.LogInformation($"Successfully parsed {entities.Count} entities from URL");
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DWG from URL");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<string> DownloadFileFromUrl(string url)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".dwg");

            using (var client = new HttpClient())
            {
                // إضافة headers إذا لزم الأمر (مثل التوثيق)
                if (url.Contains("authentication-required"))
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_TOKEN");
                }

                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }
            }

            return tempPath;
        }
    }
}
