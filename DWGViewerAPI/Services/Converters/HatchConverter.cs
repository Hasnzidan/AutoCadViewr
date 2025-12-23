using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class HatchConverter : IEntityTypeConverter
    {
        private readonly IBoundaryLoopService _boundaryLoopService;
        
        public HatchConverter(IBoundaryLoopService boundaryLoopService)
        {
            _boundaryLoopService = boundaryLoopService;
        }
        
        public bool CanConvert(Entity entity) => entity is Hatch;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var hatch = (Hatch)entity;
            result.Type = "Hatch";

            var boundaries = new List<List<double[]>>();

            foreach (var boundary in hatch.Paths)
            {
                // Use shared service to process edges
                var boundaryPoints = _boundaryLoopService.ProcessEdges(boundary.Edges, "Hatch");
                
                // Use shared service to finalize loop (deduplicate + close)
                boundaryPoints = _boundaryLoopService.FinalizeLoop(boundaryPoints);

                if (boundaryPoints.Count > 0)
                {
                    boundaries.Add(boundaryPoints);
                }
            }

            result.Geometry = new HatchGeometry
            {
                Boundaries = boundaries,
                PatternName = hatch.Pattern?.Name ?? "SOLID",
                PatternType = hatch.PatternType.ToString(),
                IsSolid = hatch.IsSolid,
            };

            result.DwgProperties.Add("PatternName", hatch.Pattern?.Name ?? "SOLID");
            result.DwgProperties.Add("PatternType", hatch.PatternType.ToString());
            result.DwgProperties.Add("IsSolid", hatch.IsSolid);
            result.DwgProperties.Add("Associative", hatch.IsAssociative);
            result.DwgProperties.Add("BoundaryCount", boundaries.Count);
        }
    }
}