using DWGViewerAPI.Models.Entities;

namespace DWGViewerAPI.Models
{
    public class DwgFileResult
    {
        public List<DwgEntity> Entities { get; set; } = new();
        public List<DwgLayer> Layers { get; set; } = new();
        public List<DwgLinetype> Linetypes { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class DwgLayer
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty; // RGB string
        public bool IsVisible { get; set; } = true;
        public bool IsFrozen { get; set; } = false;
        public string Handle { get; set; } = string.Empty;
    }

    public class DwgLinetype
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<double> Pattern { get; set; } = new();
    }
}
