using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class EllipseConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Ellipse;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var ellipse = (Ellipse)entity;
            result.Type = "Ellipse";

            // MajorAxis is the vector from center to the end of major axis
            double majorAxisLength = Math.Sqrt(
                Math.Pow(ellipse.MajorAxisEndPoint.X, 2)
                    + Math.Pow(ellipse.MajorAxisEndPoint.Y, 2)
                    + Math.Pow(ellipse.MajorAxisEndPoint.Z, 2)
            );

            double minorAxisLength = ellipse.RadiusRatio * majorAxisLength;

            result.Geometry = new EllipseGeometry
            {
                Center = new[] { ellipse.Center.X, ellipse.Center.Y, ellipse.Center.Z },
                MajorAxis = new[]
                {
                    ellipse.MajorAxisEndPoint.X,
                    ellipse.MajorAxisEndPoint.Y,
                    ellipse.MajorAxisEndPoint.Z,
                },
                MinorAxisRatio = ellipse.RadiusRatio,
                StartAngle = ellipse.StartParameter,
                EndAngle = ellipse.EndParameter,
            };

            result.DwgProperties.Add(
                "Center",
                $"{ellipse.Center.X:F2}, {ellipse.Center.Y:F2}, {ellipse.Center.Z:F2}"
            );
            result.DwgProperties.Add("MajorAxisLength", majorAxisLength);
            result.DwgProperties.Add("MinorAxisLength", minorAxisLength);
            result.DwgProperties.Add("MinorAxisRatio", ellipse.RadiusRatio);
            result.DwgProperties.Add("StartAngle", ellipse.StartParameter * (180 / Math.PI));
            result.DwgProperties.Add("EndAngle", ellipse.EndParameter * (180 / Math.PI));
        }
    }
}
