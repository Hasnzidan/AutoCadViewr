using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class CircleConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Circle;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var circle = (Circle)entity;
            result.Type = "Circle";
            result.Geometry = new CircleGeometry
            {
                Center = new[] { circle.Center.X, circle.Center.Y, circle.Center.Z },
                Radius = circle.Radius
            };

            result.DwgProperties.Add("Thickness", circle.Thickness);
            result.DwgProperties.Add("Center", $"{circle.Center.X:F2}, {circle.Center.Y:F2}, {circle.Center.Z:F2}");
            result.DwgProperties.Add("Radius", circle.Radius);
            result.DwgProperties.Add("Diameter", circle.Radius * 2);
            result.DwgProperties.Add("Area", Math.PI * Math.Pow(circle.Radius, 2));
            result.DwgProperties.Add("Circumference", 2 * Math.PI * circle.Radius);
        }
    }
}
