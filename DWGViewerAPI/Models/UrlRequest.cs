namespace DWGViewerAPI.Models
{
    public class UrlRequest
    {
        public required string Url { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}
