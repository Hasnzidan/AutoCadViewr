using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using DWGViewerAPI.Models;

namespace DWGViewerAPI.Services
{
    public class DwgParserService
    {
        public List<DwgEntity> ParseDwgFile(string filePath)
        {
            var entities = new List<DwgEntity>();

            try
            {
                // فتح ملف DWG باستخدام ACadSharp
                using (DwgReader reader = new DwgReader(filePath))
                {
                    CadDocument doc = reader.Read();

                    // المرور على كل الكيانات في الملف
                    foreach (var entity in doc.Entities)
                    {
                        var dwgEntity = ConvertEntity(entity);
                        if (dwgEntity != null)
                        {
                            entities.Add(dwgEntity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing DWG file: {ex.Message}", ex);
            }

            return entities;
        }

        private DwgEntity? ConvertEntity(Entity entity)
        {
            var dwgEntity = new DwgEntity
            {
                Id = entity.Handle.ToString(),
                DwgProperties = new Dictionary<string, object>
                {
                    { "Handle", entity.Handle.ToString() },
                    { "Layer", entity.Layer?.Name ?? "0" },
                    { "Color", $"{entity.Color.R}, {entity.Color.G}, {entity.Color.B}" }
                }
            };

            // معالجة حسب نوع الكيان (الترتيب مهم: الأنواع الفرعية قبل الأساسية)
            switch (entity)
            {
                case Line line:
                    dwgEntity.Type = "Line";
                    dwgEntity.Geometry = new LineGeometry
                    {
                        Points = new List<double[]>
                        {
                            new[] { line.StartPoint.X, line.StartPoint.Y, line.StartPoint.Z },
                            new[] { line.EndPoint.X, line.EndPoint.Y, line.EndPoint.Z }
                        }
                    };
                    break;

                case Arc arc:
                    dwgEntity.Type = "Arc";
                    dwgEntity.Geometry = new ArcGeometry
                    {
                        Center = new[] { arc.Center.X, arc.Center.Y, arc.Center.Z },
                        Radius = arc.Radius,
                        StartAngle = arc.StartAngle,
                        EndAngle = arc.EndAngle
                    };
                    break;

                case Circle circle:
                    dwgEntity.Type = "Circle";
                    dwgEntity.Geometry = new CircleGeometry
                    {
                        Center = new[] { circle.Center.X, circle.Center.Y, circle.Center.Z },
                        Radius = circle.Radius
                    };
                    break;

                default:
                    // أنواع أخرى غير مدعومة حالياً
                    return null;
            }

            // إضافة XData إذا كانت موجودة
            if (entity.ExtendedData != null && entity.ExtendedData.Any())
            {
                foreach (var xdata in entity.ExtendedData)
                {
                    dwgEntity.DwgProperties.Add($"XData_{xdata.Key}", xdata.Value.ToString() ?? "");
                }
            }

            return dwgEntity;
        }
    }
}
