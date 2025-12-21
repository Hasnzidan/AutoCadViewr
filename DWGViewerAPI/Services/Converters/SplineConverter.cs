using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class SplineConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Spline;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var spline = (Spline)entity;
            result.Type = "Spline";
            
            var controlPoints = new List<double[]>();
            foreach (var point in spline.ControlPoints)
            {
                controlPoints.Add(new[] { point.X, point.Y, point.Z });
            }
            
            var fitPoints = new List<double[]>();
            foreach (var point in spline.FitPoints)
            {
                fitPoints.Add(new[] { point.X, point.Y, point.Z });
            }
            
            var knots = spline.Knots.ToList();
            
            result.Geometry = new SplineGeometry
            {
                ControlPoints = controlPoints,
                FitPoints = fitPoints,
                Knots = knots,
                Degree = spline.Degree,
                IsClosed = spline.Flags.HasFlag(SplineFlags.Closed),
                IsPeriodic = spline.Flags.HasFlag(SplineFlags.Periodic),
                IsRational = spline.Flags.HasFlag(SplineFlags.Rational)
            };

            result.DwgProperties.Add("Degree", spline.Degree);
            result.DwgProperties.Add("IsClosed", spline.Flags.HasFlag(SplineFlags.Closed));
            result.DwgProperties.Add("IsPeriodic", spline.Flags.HasFlag(SplineFlags.Periodic));
            result.DwgProperties.Add("IsRational", spline.Flags.HasFlag(SplineFlags.Rational));
            result.DwgProperties.Add("ControlPointCount", controlPoints.Count);
            result.DwgProperties.Add("FitPointCount", fitPoints.Count);
            result.DwgProperties.Add("KnotCount", knots.Count);
        }
    }
}