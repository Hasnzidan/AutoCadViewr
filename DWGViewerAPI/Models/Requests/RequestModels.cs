namespace DWGViewerAPI.Models.Requests
{
    public class UrlRequest
    {
        public string Url { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    public class Base64Request
    {
        public string Base64Data { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/acad";
    }
}
