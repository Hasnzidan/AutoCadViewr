using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class ArcConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Arc;

        public void Convert(Entity entity, DwgEntity result)
        {
            var arc = (Arc)entity;
            result.Type = "Arc";
            result.Geometry = new ArcGeometry
            {
                Center = new[] { arc.Center.X, arc.Center.Y, arc.Center.Z },
                Radius = arc.Radius,
                StartAngle = arc.StartAngle,
                EndAngle = arc.EndAngle
            };

            result.DwgProperties.Add("Thickness", arc.Thickness);
            result.DwgProperties.Add("Center", $"{arc.Center.X:F2}, {arc.Center.Y:F2}, {arc.Center.Z:F2}");
            result.DwgProperties.Add("Radius", arc.Radius);
            result.DwgProperties.Add("Start Angle", arc.StartAngle * (180 / Math.PI));
            result.DwgProperties.Add("End Angle", arc.EndAngle * (180 / Math.PI));
            double arcLength = arc.Radius * Math.Abs(arc.EndAngle - arc.StartAngle);
            result.DwgProperties.Add("Arc Length", arcLength);
        }
    }
}
