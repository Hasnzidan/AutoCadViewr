using ACadSharp;
using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class MLineConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is MLine;

        public void Convert(Entity entity, DwgEntity result, CadDocument doc)
        {
            var mline = (MLine)entity;
            result.Type = "MLine";

            var geometry = new PolylineGeometry();
            geometry.Vertices = new List<double[]>();

            // Extract vertices from MLine
            try
            {
                if (mline.Vertices != null && mline.Vertices.Count > 0)
                {
                    foreach (var vertex in mline.Vertices)
                    {
                        geometry.Vertices.Add(new[] { vertex.Position.X, vertex.Position.Y, vertex.Position.Z });
                    }
                }
            }
            catch
            {
                // Fallback: try dynamic access
                try
                {
                    dynamic dMLine = mline;
                    foreach (var v in dMLine.Vertices)
                    {
                        geometry.Vertices.Add(new[] { (double)v.Position.X, (double)v.Position.Y, (double)v.Position.Z });
                    }
                }
                catch { }
            }

            result.Geometry = geometry;

            // Add MLine-specific properties using dynamic for safety
            try
            {
                dynamic dMLine = mline;
                result.DwgProperties.Add("Scale", (double)dMLine.ScaleFactor);
                result.DwgProperties.Add("Justification", dMLine.Justification.ToString());
            }
            catch
            {
                result.DwgProperties.Add("Scale", 1.0);
                result.DwgProperties.Add("Justification", "Top");
            }
            result.DwgProperties.Add("VertexCount", geometry.Vertices.Count);
            
            try
            {
                result.DwgProperties.Add("StyleName", mline.Style?.Name ?? "STANDARD");
            }
            catch
            {
                result.DwgProperties.Add("StyleName", "STANDARD");
            }
        }
    }
}
