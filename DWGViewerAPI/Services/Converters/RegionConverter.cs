using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class RegionConverter : IEntityTypeConverter
    {
        private readonly IBoundaryLoopService _boundaryLoopService;

        public RegionConverter(IBoundaryLoopService boundaryLoopService)
        {
            _boundaryLoopService = boundaryLoopService;
        }

        public bool CanConvert(Entity entity) => entity is Region;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var region = (Region)entity;
            result.Type = "Region";

            var boundaries = new List<List<double[]>>();

            result.Geometry = new RegionGeometry
            {
                Boundaries = boundaries,
                IsSolid = true,
            };

            result.DwgProperties.Add("IsSolid", true);
            result.DwgProperties.Add("BoundaryCount", boundaries.Count);
            result.DwgProperties.Add("Note", "Region boundary extraction pending investigation");
        }
    }
}