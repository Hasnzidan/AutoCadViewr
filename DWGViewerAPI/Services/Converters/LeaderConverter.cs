using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class LeaderConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Leader || entity is MultiLeader;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            if (entity is Leader leader)
            {
                result.Type = "Leader";
                
                var vertices = new List<double[]>();
                foreach (var vertex in leader.Vertices)
                {
                    vertices.Add(new[] { vertex.X, vertex.Y, vertex.Z });
                }
                
                result.Geometry = new LeaderGeometry
                {
                    Vertices = vertices,
                    HasArrowhead = leader.ArrowHeadEnabled,
                    HasHookline = leader.PathType == LeaderPathType.Spline
                };

                result.DwgProperties.Add("VertexCount", vertices.Count);
                result.DwgProperties.Add("HasArrowhead", leader.ArrowHeadEnabled);
                result.DwgProperties.Add("PathType", leader.PathType.ToString());
                result.DwgProperties.Add("DimensionStyle", leader.Style?.Name ?? "Standard");
            }
            else if (entity is MultiLeader mleader)
            {
                result.Type = "MultiLeader";
                
                result.Geometry = new MultiLeaderGeometry
                {
                    LandingLocation = new[] { 0.0, 0.0, 0.0 },
                    DoglegLength = 0.0
                };

                result.DwgProperties.Add("LeaderLineType", mleader.PathType.ToString());
                result.DwgProperties.Add("Style", mleader.Style?.Name ?? "Standard");
            }
        }
    }
}