using System.Net;

namespace DWGViewerAPI.Infrastructure
{
    public class FileDownloader
    {
        private readonly HttpClient _httpClient;

        public FileDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> DownloadFileAsync(string url)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".dwg");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                if (errorContent.Contains("ExpiredToken") || errorContent.Contains("AccessDenied"))
                {
                    throw new Exception("رابط AWS منتهي الصلاحية. الرجاء الحصول على رابط جديد.");
                }
            }

            response.EnsureSuccessStatusCode();

            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fileStream);

            return tempPath;
        }
    }
}
