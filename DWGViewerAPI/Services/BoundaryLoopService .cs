using ACadSharp.Entities;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services
{
    public class BoundaryLoopService : IBoundaryLoopService
    {
        public List<double[]> ProcessEdges(IEnumerable<object> edges, string entityType)
        {
            var points = new List<double[]>();
            int edgeIndex = 0;

            foreach (var edge in edges)
            {
                edgeIndex++;
                string edgeTypeName = edge.GetType().Name;
                Console.WriteLine($"    Edge #{edgeIndex}: {edgeTypeName}");

                // Line Edge
                if (edge is Hatch.BoundaryPath.Line line)
                {
                    points.Add(new[] { line.Start.X, line.Start.Y });
                }
                // Arc Edge
                else if (edge is Hatch.BoundaryPath.Arc arc)
                {
                    points.AddRange(SampleArc(arc));
                }
                // Ellipse Edge
                else if (edge is Hatch.BoundaryPath.Ellipse ellipse)
                {
                    points.AddRange(SampleEllipse(ellipse));
                }
                // Polyline Edge
                else if (edge is Hatch.BoundaryPath.Polyline polyline)
                {
                    foreach (var vertex in polyline.Vertices)
                    {
                        points.Add(new[] { vertex.X, vertex.Y });
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"      ⚠️  UNSUPPORTED EDGE TYPE: {edgeTypeName}");
                    Console.ResetColor();
                }
            }

            return points;
        }

        public List<double[]> FinalizeLoop(List<double[]> points)
        {
            // Remove duplicates
            points = RemoveConsecutiveDuplicates(points);

            // Ensure closure
            if (points.Count > 2)
            {
                var first = points[0];
                var last = points[^1];
                double distance = Math.Sqrt(
                    Math.Pow(first[0] - last[0], 2) + 
                    Math.Pow(first[1] - last[1], 2)
                );

                if (distance > 1e-6)
                {
                    points.Add(new[] { first[0], first[1] });
                }
            }

            return points;
        }

        private List<double[]> SampleArc(Hatch.BoundaryPath.Arc arc)
        {
            var points = new List<double[]>();
            int segments = 16;
            double startAngle = arc.StartAngle;
            double endAngle = arc.EndAngle;

            if (endAngle < startAngle)
                endAngle += 2 * Math.PI;

            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double angle = startAngle + (endAngle - startAngle) * t;
                double x = arc.Center.X + arc.Radius * Math.Cos(angle);
                double y = arc.Center.Y + arc.Radius * Math.Sin(angle);
                points.Add(new[] { x, y });
            }

            return points;
        }

        private List<double[]> SampleEllipse(Hatch.BoundaryPath.Ellipse ellipse)
        {
            var points = new List<double[]>();
            int segments = 64;
            var center = ellipse.Center;
            var majorAxis = ellipse.MajorAxisEndPoint;

            dynamic dynamicEllipse = ellipse;
            double ratio = 1.0;
            try { ratio = (double)dynamicEllipse.MinorAxisRatio; } catch { }
            try { if (ratio == 1.0) ratio = (double)dynamicEllipse.Ratio; } catch { }

            double minorX = -majorAxis.Y * ratio;
            double minorY = majorAxis.X * ratio;

            for (int i = 0; i <= segments; i++)
            {
                double angle = ellipse.StartAngle + (ellipse.EndAngle - ellipse.StartAngle) * i / segments;
                double cos = Math.Cos(angle);
                double sin = Math.Sin(angle);

                double x = center.X + (majorAxis.X * cos + minorX * sin);
                double y = center.Y + (majorAxis.Y * cos + minorY * sin);
                points.Add(new[] { x, y });
            }

            return points;
        }

        private static List<double[]> RemoveConsecutiveDuplicates(List<double[]> points)
        {
            if (points.Count <= 1) return points;

            var result = new List<double[]> { points[0] };
            const double tolerance = 1e-9;

            for (int i = 1; i < points.Count; i++)
            {
                var prev = result[^1];
                var curr = points[i];

                double dx = curr[0] - prev[0];
                double dy = curr[1] - prev[1];
                double distanceSquared = dx * dx + dy * dy;

                if (distanceSquared > tolerance * tolerance)
                {
                    result.Add(curr);
                }
            }

            return result;
        }
    }
}