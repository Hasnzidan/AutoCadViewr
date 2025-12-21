using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class HatchConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Hatch;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var hatch = (Hatch)entity;
            result.Type = "Hatch";
            
            var boundaries = new List<List<double[]>>();
            
            foreach (var boundary in hatch.Paths)
            {
                var boundaryPoints = new List<double[]>();
                
                foreach (var edge in boundary.Edges)
                {
                    if (edge is Hatch.BoundaryPath.Line line)
                    {
                        boundaryPoints.Add(new[] { line.Start.X, line.Start.Y });
                        boundaryPoints.Add(new[] { line.End.X, line.End.Y });
                    }
                    else if (edge is Hatch.BoundaryPath.Arc arc)
                    {
                        int segments = 16;
                        for (int i = 0; i <= segments; i++)
                        {
                            double angle = arc.StartAngle + (arc.EndAngle - arc.StartAngle) * i / segments;
                            double x = arc.Center.X + arc.Radius * Math.Cos(angle);
                            double y = arc.Center.Y + arc.Radius * Math.Sin(angle);
                            boundaryPoints.Add(new[] { x, y });
                        }
                    }
                    else if (edge is Hatch.BoundaryPath.Ellipse ellipse)
                    {
                        int segments = 64;
                        var center = ellipse.Center;
                        var majorAxis = ellipse.MajorAxisEndPoint; 
                        
                        dynamic dynamicEllipse = ellipse;
                        double ratio = 1.0;
                        try { ratio = (double)dynamicEllipse.MinorAxisRatio; } catch { }
                        try { if (ratio == 1.0) ratio = (double)dynamicEllipse.Ratio; } catch { }

                        // Minor axis vector
                        double minorX = -majorAxis.Y * ratio;
                        double minorY = majorAxis.X * ratio;

                        for (int i = 0; i <= segments; i++)
                        {
                            double angle = ellipse.StartAngle + (ellipse.EndAngle - ellipse.StartAngle) * i / segments;
                            double cos = Math.Cos(angle);
                            double sin = Math.Sin(angle);
                            
                            double x = center.X + (majorAxis.X * cos + minorX * sin);
                            double y = center.Y + (majorAxis.Y * cos + minorY * sin);
                            boundaryPoints.Add(new[] { x, y });
                        }
                    }
                }
                
                if (boundaryPoints.Count > 0)
                {
                    boundaries.Add(boundaryPoints);
                }
            }
            
            result.Geometry = new HatchGeometry
            {
                Boundaries = boundaries,
                PatternName = hatch.Pattern?.Name ?? "SOLID",
                PatternType = hatch.PatternType.ToString(),
                IsSolid = hatch.IsSolid
            };

            result.DwgProperties.Add("PatternName", hatch.Pattern?.Name ?? "SOLID");
            result.DwgProperties.Add("PatternType", hatch.PatternType.ToString());
            result.DwgProperties.Add("IsSolid", hatch.IsSolid);
            result.DwgProperties.Add("Associative", hatch.IsAssociative);
            result.DwgProperties.Add("BoundaryCount", boundaries.Count);
        }
    }
}