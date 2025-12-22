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

            // Check if this circle has arc properties (StartAngle, EndAngle)
            // Try to access Arc-specific properties using reflection
            var circleType = circle.GetType();
            var startAngleProp = circleType.GetProperty("StartAngle");
            var endAngleProp = circleType.GetProperty("EndAngle");

            bool isActuallyArc = false;
            double startAngle = 0;
            double endAngle = 0;

            if (startAngleProp != null && endAngleProp != null)
            {
                try
                {
                    var startAngleObj = startAngleProp.GetValue(circle);
                    var endAngleObj = endAngleProp.GetValue(circle);

                    if (startAngleObj != null && endAngleObj != null)
                    {
                        startAngle = (double)startAngleObj;
                        endAngle = (double)endAngleObj;

                        // Check if it's truly an arc (not a full circle)
                        double arcAngle = endAngle - startAngle;
                        if (arcAngle < 0)
                            arcAngle += 2 * Math.PI;

                        // If the arc angle is not close to 2π, it's an arc
                        isActuallyArc = Math.Abs(arcAngle - 2 * Math.PI) > 0.01;
                    }
                }
                catch { }
            }

            // If it's an arc, handle it as arc
            if (isActuallyArc)
            {
                result.Type = "Arc";

                // Normalize angles
                startAngle = NormalizeAngle(startAngle);
                endAngle = NormalizeAngle(endAngle);

                double arcAngle = endAngle - startAngle;
                if (arcAngle <= 0)
                    arcAngle += 2 * Math.PI;

                result.Geometry = new ArcGeometry
                {
                    Center = new[] { circle.Center.X, circle.Center.Y, circle.Center.Z },
                    Radius = circle.Radius,
                    StartAngle = startAngle,
                    EndAngle = endAngle,
                    Normal = new[] { circle.Normal.X, circle.Normal.Y, circle.Normal.Z },
                };

                result.DwgProperties.Add("Thickness", circle.Thickness);
                result.DwgProperties.Add(
                    "Center",
                    $"{circle.Center.X:F2}, {circle.Center.Y:F2}, {circle.Center.Z:F2}"
                );
                result.DwgProperties.Add("Radius", circle.Radius);
                result.DwgProperties.Add("Start Angle", startAngle * (180 / Math.PI));
                result.DwgProperties.Add("End Angle", endAngle * (180 / Math.PI));
                double arcLength = circle.Radius * Math.Abs(arcAngle);
                result.DwgProperties.Add("Arc Length", arcLength);
            }
            else
            {
                // It's actually a circle
                result.Type = "Circle";
                result.Geometry = new CircleGeometry
                {
                    Center = new[] { circle.Center.X, circle.Center.Y, circle.Center.Z },
                    Radius = circle.Radius
                };

                result.DwgProperties.Add("Thickness", circle.Thickness);
                result.DwgProperties.Add(
                    "Center",
                    $"{circle.Center.X:F2}, {circle.Center.Y:F2}, {circle.Center.Z:F2}"
                );
                result.DwgProperties.Add("Radius", circle.Radius);
                result.DwgProperties.Add("Diameter", circle.Radius * 2);
                result.DwgProperties.Add("Area", Math.PI * Math.Pow(circle.Radius, 2));
                result.DwgProperties.Add("Circumference", 2 * Math.PI * circle.Radius);
            }
        }

        private double NormalizeAngle(double angle)
        {
            while (angle < 0)
                angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI)
                angle -= 2 * Math.PI;
            return angle;
        }
    }
}
