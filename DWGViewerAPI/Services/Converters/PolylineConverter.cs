using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class PolylineConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is LwPolyline || entity.GetType().Name.Contains("Polyline");

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            result.Type = "Polyline";
            var geometry = new PolylineGeometry();
            dynamic dynamicEntity = entity;

            if (entity is LwPolyline lwPoly)
            {
                geometry.IsClosed = lwPoly.IsClosed;
                foreach (var vertex in lwPoly.Vertices)
                {
                    geometry.Vertices.Add(new[] { vertex.Location.X, vertex.Location.Y, lwPoly.Elevation });
                    geometry.Bulges.Add(vertex.Bulge);
                }
            }
            else
            {
                // Handle Polyline2D, Polyline3D, etc. via dynamic
                try
                {
                    // Use dynamic to check for 'Closed' property or flag
                    geometry.IsClosed = ((int)dynamicEntity.Flags & 1) != 0; 
                    foreach (var vertex in dynamicEntity.Vertices)
                    {
                        var pos = vertex.Position;
                        geometry.Vertices.Add(new double[] { (double)pos.X, (double)pos.Y, (double)pos.Z });
                        geometry.Bulges.Add(0.0);
                    }
                }
                catch { }
            }

            result.Geometry = geometry;
            result.DwgProperties.Add("IsClosed", geometry.IsClosed);
            result.DwgProperties.Add("VertexCount", geometry.Vertices.Count);
        }
    }
}