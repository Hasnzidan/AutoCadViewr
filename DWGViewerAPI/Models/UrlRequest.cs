namespace DWGViewerAPI.Models
{
    public class UrlRequest
    {
        public string Url { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}