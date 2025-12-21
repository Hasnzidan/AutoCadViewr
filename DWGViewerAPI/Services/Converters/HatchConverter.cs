using ACadSharp;
using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class HatchConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Hatch;

        public void Convert(Entity entity, DwgEntity result, CadDocument doc)
        {
            var hatch = (Hatch)entity;
            result.Type = "Hatch";

            var geometry = new HatchGeometry();
            
            // Debug: Capture all properties to help identify missing data
            var propNames = hatch.GetType().GetProperties().Select(p => p.Name);
            result.DwgProperties.Add("_Debug_HatchProps", string.Join(", ", propNames));

            // Extract boundary paths safely
            try
            {
                foreach (var path in hatch.Paths)
                {
                    var loopPoints = new List<double[]>();
                    dynamic dPath = path;

                    // Try to handle Polyline-style loops
                    try {
                        if (dPath.Vertices != null) {
                            foreach (var v in dPath.Vertices) {
                                loopPoints.Add(new[] { (double)v.X, (double)v.Y, 0.0 });
                            }
                        }
                    } catch {}

                    // Try to handle Edge-style loops
                    try {
                        if (dPath.Edges != null) {
                            foreach (var edge in dPath.Edges) {
                                try {
                                    // Use dynamic to get any property that looks like a point
                                    loopPoints.Add(new[] { (double)edge.StartPoint.X, (double)edge.StartPoint.Y, 0.0 });
                                    loopPoints.Add(new[] { (double)edge.EndPoint.X, (double)edge.EndPoint.Y, 0.0 });
                                } catch {
                                    try {
                                        loopPoints.Add(new[] { (double)edge.Center.X, (double)edge.Center.Y, 0.0 });
                                    } catch {}
                                }
                            }
                        }
                    } catch {}

                    if (loopPoints.Count > 0) geometry.Paths.Add(loopPoints);
                }
            }
            catch { }

            // Try to get hatch lines via Explode safely
            int explodedCount = 0;
            try
            {
                var explodeMethod = hatch.GetType().GetMethod("Explode");
                if (explodeMethod != null)
                {
                    var explodedEntities = explodeMethod.Invoke(hatch, null) as System.Collections.IEnumerable;
                    if (explodedEntities != null)
                    {
                        foreach (var exp in explodedEntities)
                        {
                            explodedCount++;
                            dynamic dExp = exp;
                            try {
                                geometry.PatternLines.Add(new[] { (double)dExp.StartPoint.X, (double)dExp.StartPoint.Y, (double)dExp.StartPoint.Z });
                                geometry.PatternLines.Add(new[] { (double)dExp.EndPoint.X, (double)dExp.EndPoint.Y, (double)dExp.EndPoint.Z });
                            } catch {}
                        }
                    }
                }
            }
            catch { }

            // Diagnostic: Check Pattern.Lines properties
            if (hatch.Pattern?.Lines != null && hatch.Pattern.Lines.Count > 0)
            {
                var firstLine = hatch.Pattern.Lines[0];
                var lineProps = firstLine.GetType().GetProperties().Select(p => p.Name);
                result.DwgProperties.Add("_PatternLineProps", string.Join(", ", lineProps));
                result.DwgProperties.Add("_PatternLinesCount", hatch.Pattern.Lines.Count);
                
                // Try to extract pattern line data
                try
                {
                    dynamic dLine = firstLine;
                    result.DwgProperties.Add("_PatternLine_BasePoint", $"{dLine.BasePoint.X}, {dLine.BasePoint.Y}");
                    result.DwgProperties.Add("_PatternLine_Offset", $"{dLine.Offset.X}, {dLine.Offset.Y}");
                    result.DwgProperties.Add("_PatternLine_Angle", dLine.Angle.ToString());
                }
                catch (Exception ex)
                {
                    result.DwgProperties.Add("_PatternLine_Error", ex.Message);
                }
            }

            result.DwgProperties.Add("PatternName", hatch.Pattern?.Name ?? "Solid");
            result.DwgProperties.Add("PatternAngle", hatch.PatternAngle);
            result.DwgProperties.Add("PatternScale", hatch.PatternScale);
            result.DwgProperties.Add("IsSolid", hatch.IsSolid);
            result.DwgProperties.Add("_ExplodedCount", explodedCount);

            result.Geometry = geometry;
        }
    }
}
