namespace DWGViewerAPI.Models.Entities
{
    public class DwgEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public object Geometry { get; set; } = new { };
        public Dictionary<string, object> DwgProperties { get; set; } = new();
    }
}
