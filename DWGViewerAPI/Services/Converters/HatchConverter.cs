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

            Console.WriteLine($"\n========== HATCH CONVERSION START ==========");
            Console.WriteLine($"Hatch Handle: {hatch.Handle}");
            Console.WriteLine($"Pattern: {hatch.Pattern?.Name ?? "SOLID"}");
            Console.WriteLine($"IsSolid: {hatch.IsSolid}");
            Console.WriteLine($"Total Boundary Paths: {hatch.Paths.Count}");

            var boundaries = new List<List<double[]>>();
            int boundaryIndex = 0;

            foreach (var boundary in hatch.Paths)
            {
                boundaryIndex++;
                Console.WriteLine($"\n--- Boundary Path #{boundaryIndex} ---");
                Console.WriteLine($"  Total Edges: {boundary.Edges.Count}");

                var boundaryPoints = new List<double[]>();
                int edgeIndex = 0;

                foreach (var edge in boundary.Edges)
                {
                    edgeIndex++;
                    string edgeTypeName = edge.GetType().Name;
                    Console.WriteLine($"    Edge #{edgeIndex}: {edgeTypeName}");

                    if (edge is Hatch.BoundaryPath.Line line)
                    {
                        Console.WriteLine($"      Line: ({line.Start.X:F2}, {line.Start.Y:F2}) → ({line.End.X:F2}, {line.End.Y:F2})");
                        // Only add start point to avoid duplicates (next edge will add its start)
                        boundaryPoints.Add(new[] { line.Start.X, line.Start.Y });
                    }
                    else if (edge is Hatch.BoundaryPath.Arc arc)
                    {
                        Console.WriteLine($"      Arc: Center=({arc.Center.X:F2}, {arc.Center.Y:F2}), Radius={arc.Radius:F2}");
                        Console.WriteLine($"           StartAngle={arc.StartAngle:F4}, EndAngle={arc.EndAngle:F4}");
                        
                        int segments = 16;
                        double startAngle = arc.StartAngle;
                        double endAngle = arc.EndAngle;
                        
                        // Handle angle wrapping (e.g., 350° to 10°)
                        if (endAngle < startAngle)
                            endAngle += 2 * Math.PI;
                        
                        for (int i = 0; i <= segments; i++)
                        {
                            double t = (double)i / segments;
                            double angle = startAngle + (endAngle - startAngle) * t;
                            double x = arc.Center.X + arc.Radius * Math.Cos(angle);
                            double y = arc.Center.Y + arc.Radius * Math.Sin(angle);
                            boundaryPoints.Add(new[] { x, y });
                        }
                        Console.WriteLine($"      → Sampled to {segments + 1} points");
                    }
                    else if (edge is Hatch.BoundaryPath.Ellipse ellipse)
                    {
                        Console.WriteLine($"      Ellipse: Center=({ellipse.Center.X:F2}, {ellipse.Center.Y:F2})");
                        Console.WriteLine($"               MajorAxis=({ellipse.MajorAxisEndPoint.X:F2}, {ellipse.MajorAxisEndPoint.Y:F2})");
                        
                        int segments = 64;
                        var center = ellipse.Center;
                        var majorAxis = ellipse.MajorAxisEndPoint;

                        dynamic dynamicEllipse = ellipse;
                        double ratio = 1.0;
                        try
                        {
                            ratio = (double)dynamicEllipse.MinorAxisRatio;
                        }
                        catch { }
                        try
                        {
                            if (ratio == 1.0)
                                ratio = (double)dynamicEllipse.Ratio;
                        }
                        catch { }

                        Console.WriteLine($"               MinorAxisRatio={ratio:F4}");

                        // Minor axis vector
                        double minorX = -majorAxis.Y * ratio;
                        double minorY = majorAxis.X * ratio;

                        for (int i = 0; i <= segments; i++)
                        {
                            double angle =
                                ellipse.StartAngle
                                + (ellipse.EndAngle - ellipse.StartAngle) * i / segments;
                            double cos = Math.Cos(angle);
                            double sin = Math.Sin(angle);

                            double x = center.X + (majorAxis.X * cos + minorX * sin);
                            double y = center.Y + (majorAxis.Y * cos + minorY * sin);
                            boundaryPoints.Add(new[] { x, y });
                        }
                        Console.WriteLine($"      → Sampled to {segments + 1} points");
                    }
                    else if (edge is Hatch.BoundaryPath.Polyline polyline)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"      Polyline: {polyline.Vertices.Count} vertices");
                        Console.ResetColor();
                        
                        foreach (var vertex in polyline.Vertices)
                        {
                            boundaryPoints.Add(new[] { vertex.X, vertex.Y });
                        }
                        
                        Console.WriteLine($"      → Added {polyline.Vertices.Count} points");
                    }
                    else
                    {
                        // Unknown/Unsupported edge type
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"      ⚠️  UNSUPPORTED EDGE TYPE: {edgeTypeName}");
                        Console.WriteLine($"          Full Type: {edge.GetType().FullName}");
                        Console.ResetColor();
                    }
                }

                Console.WriteLine($"  Boundary #{boundaryIndex} Total Points (before cleanup): {boundaryPoints.Count}");

                // Remove consecutive duplicate points
                boundaryPoints = RemoveConsecutiveDuplicates(boundaryPoints);
                
                Console.WriteLine($"  Boundary #{boundaryIndex} Total Points (after cleanup): {boundaryPoints.Count}");

                // Check if loop is closed
                if (boundaryPoints.Count > 0)
                {
                    var first = boundaryPoints[0];
                    var last = boundaryPoints[^1];
                    double distance = Math.Sqrt(
                        Math.Pow(first[0] - last[0], 2) + 
                        Math.Pow(first[1] - last[1], 2)
                    );
                    
                    bool isClosed = distance < 1e-6;
                    
                    // If not closed, force closure by adding first point
                    if (!isClosed && boundaryPoints.Count > 2)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  Loop NOT closed (distance: {distance:E2}), forcing closure...");
                        Console.ResetColor();
                        boundaryPoints.Add(new[] { first[0], first[1] });
                        isClosed = true;
                    }
                    
                    Console.WriteLine($"  Loop Closed: {(isClosed ? "✓ YES" : "✗ NO")} (distance: {distance:E2})");
                    
                    boundaries.Add(boundaryPoints);
                }
            }

            Console.WriteLine($"\n========== HATCH CONVERSION END ==========");
            Console.WriteLine($"Total Boundaries Converted: {boundaries.Count}\n");

            result.Geometry = new HatchGeometry
            {
                Boundaries = boundaries,
                PatternName = hatch.Pattern?.Name ?? "SOLID",
                PatternType = hatch.PatternType.ToString(),
                IsSolid = hatch.IsSolid,
            };

            result.DwgProperties.Add("PatternName", hatch.Pattern?.Name ?? "SOLID");
            result.DwgProperties.Add("PatternType", hatch.PatternType.ToString());
            result.DwgProperties.Add("IsSolid", hatch.IsSolid);
            result.DwgProperties.Add("Associative", hatch.IsAssociative);
            result.DwgProperties.Add("BoundaryCount", boundaries.Count);
        }

        /// <summary>
        /// Removes consecutive duplicate points from a list of points
        /// </summary>
        private static List<double[]> RemoveConsecutiveDuplicates(List<double[]> points)
        {
            if (points.Count <= 1)
                return points;

            var result = new List<double[]> { points[0] };
            const double tolerance = 1e-9;

            for (int i = 1; i < points.Count; i++)
            {
                var prev = result[^1];
                var curr = points[i];

                double dx = curr[0] - prev[0];
                double dy = curr[1] - prev[1];
                double distanceSquared = dx * dx + dy * dy;

                // Only add if not duplicate
                if (distanceSquared > tolerance * tolerance)
                {
                    result.Add(curr);
                }
            }

            return result;
        }

    }
}
