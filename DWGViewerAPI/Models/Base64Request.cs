namespace DWGViewerAPI.Models
{
    public class Base64Request
    {
        public string FileName { get; set; }
        public string Base64Data { get; set; }
        public string ContentType { get; set; } = "application/acad";
    }
}