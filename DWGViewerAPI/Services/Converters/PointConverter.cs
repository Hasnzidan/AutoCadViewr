using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class PointConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Point;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var point = (Point)entity;
            result.Type = "Point";
            result.Geometry = new PointGeometry
            {
                Location = new[] { point.Location.X, point.Location.Y, point.Location.Z },
            };

            result.DwgProperties.Add(
                "Location",
                $"{point.Location.X:F2}, {point.Location.Y:F2}, {point.Location.Z:F2}"
            );
            result.DwgProperties.Add("Thickness", point.Thickness);
        }
    }
}
