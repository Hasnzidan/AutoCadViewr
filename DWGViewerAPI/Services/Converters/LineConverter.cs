using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class LineConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Line;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var line = (Line)entity;
            result.Type = "Line";
            result.Geometry = new LineGeometry
            {
                Points = new List<double[]>
                {
                    new[] { line.StartPoint.X, line.StartPoint.Y, line.StartPoint.Z },
                    new[] { line.EndPoint.X, line.EndPoint.Y, line.EndPoint.Z }
                }
            };

            result.DwgProperties.Add("Thickness", line.Thickness);
            result.DwgProperties.Add("Start Point", $"{line.StartPoint.X:F2}, {line.StartPoint.Y:F2}, {line.StartPoint.Z:F2}");
            result.DwgProperties.Add("End Point", $"{line.EndPoint.X:F2}, {line.EndPoint.Y:F2}, {line.EndPoint.Z:F2}");
            
            double length = Math.Sqrt(Math.Pow(line.EndPoint.X - line.StartPoint.X, 2) + 
                                    Math.Pow(line.EndPoint.Y - line.StartPoint.Y, 2) + 
                                    Math.Pow(line.EndPoint.Z - line.StartPoint.Z, 2));
            result.DwgProperties.Add("Length", length);
        }
    }
}
