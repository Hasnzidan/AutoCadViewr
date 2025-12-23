using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class WipeoutConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Wipeout;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var wipeout = (Wipeout)entity;
            result.Type = "Wipeout";

            var boundary = new List<double[]>();

            // Wipeout inherits from RasterImage in ACadSharp
            // Boundary vertices are stored in ClipVertices or ClippingBoundary
            
            try
            {
                // Try ClipVertices first (most common in ACadSharp)
                var clipVerticesProp = wipeout.GetType().GetProperty("ClipVertices");
                if (clipVerticesProp != null)
                {
                    var vertices = clipVerticesProp.GetValue(wipeout);
                    if (vertices != null)
                    {
                        // ClipVertices is typically IEnumerable<XY> or List<XY>
                        foreach (var vertex in (System.Collections.IEnumerable)vertices)
                        {
                            // Access X and Y properties via reflection
                            var xProp = vertex.GetType().GetProperty("X");
                            var yProp = vertex.GetType().GetProperty("Y");
                            
                            if (xProp != null && yProp != null)
                            {
                                double x = System.Convert.ToDouble(xProp.GetValue(vertex));
                                double y = System.Convert.ToDouble(yProp.GetValue(vertex));
                                boundary.Add(new[] { x, y });
                            }
                        }
                        
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Wipeout {wipeout.Handle}: Extracted {boundary.Count} vertices from ClipVertices");
                        Console.ResetColor();
                    }
                }
                else
                {
                    // Fallback: Try ClippingBoundary
                    var clippingBoundaryProp = wipeout.GetType().GetProperty("ClippingBoundary");
                    if (clippingBoundaryProp != null)
                    {
                        var vertices = clippingBoundaryProp.GetValue(wipeout);
                        if (vertices != null)
                        {
                            foreach (var vertex in (System.Collections.IEnumerable)vertices)
                            {
                                var xProp = vertex.GetType().GetProperty("X");
                                var yProp = vertex.GetType().GetProperty("Y");
                                
                                if (xProp != null && yProp != null)
                                {
                                    double x = System.Convert.ToDouble(xProp.GetValue(vertex));
                                    double y = System.Convert.ToDouble(yProp.GetValue(vertex));
                                    boundary.Add(new[] { x, y });
                                }
                            }
                            
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"✓ Wipeout {wipeout.Handle}: Extracted {boundary.Count} vertices from ClippingBoundary");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠ Wipeout {wipeout.Handle}: No ClipVertices or ClippingBoundary property found");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Wipeout {wipeout.Handle}: Error extracting vertices - {ex.Message}");
                Console.ResetColor();
            }

            result.Geometry = new WipeoutGeometry
            {
                Boundary = boundary,
                IsClipped = boundary.Count > 0,
                BoundaryType = "Polygonal"
            };

            result.DwgProperties.Add("IsClipped", boundary.Count > 0);
            result.DwgProperties.Add("BoundaryType", "Polygonal");
            result.DwgProperties.Add("VertexCount", boundary.Count);
        }
    }
}