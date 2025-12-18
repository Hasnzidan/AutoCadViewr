using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Tables;
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
                        var dwgEntity = ConvertEntity(entity, doc);
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

        private DwgEntity? ConvertEntity(Entity entity, CadDocument doc)
        {
            // الحصول على اللون الفعلي للعنصر
            var actualColor = GetActualColor(entity, doc);

            var dwgEntity = new DwgEntity
            {
                Id = entity.Handle.ToString(),
                DwgProperties = new Dictionary<string, object>
                {
                    { "Handle", entity.Handle.ToString() },
                    { "Layer", entity.Layer?.Name ?? "0" },
                    { "Color", $"{actualColor.R}, {actualColor.G}, {actualColor.B}" },
                    { "ColorIndex", entity.Color.Index },
                    { "ColorMethod", GetColorMethodName(entity.Color) }
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

        // الحصول على اللون الفعلي للعنصر مع معالجة ByLayer و ByBlock
        private ACadSharp.Color GetActualColor(Entity entity, CadDocument doc)
        {
            var color = entity.Color;

            // فحص إذا كان اللون ByLayer
            if (color.IsByLayer)
            {
                // الحصول على لون الطبقة
                if (entity.Layer != null)
                {
                    return entity.Layer.Color;
                }
                else
                {
                    // في حالة عدم وجود طبقة، استخدام اللون الأبيض كافتراضي
                    return new ACadSharp.Color(255, 255, 255);
                }
            }
            // فحص إذا كان اللون ByBlock
            else if (color.IsByBlock)
            {
                // في حالة ByBlock، نستخدم اللون الأبيض كافتراضي
                // (في التطبيقات الحقيقية، يتم استخدام لون البلوك الذي يحتوي على العنصر)
                return new ACadSharp.Color(255, 255, 255);
            }
            // اللون محدد مباشرة
            else
            {
                return color;
            }
        }

        // الحصول على اسم طريقة تحديد اللون
        private string GetColorMethodName(ACadSharp.Color color)
        {
            if (color.IsByLayer)
                return "ByLayer";
            else if (color.IsByBlock)
                return "ByBlock";
            else
                return "Direct";
        }

        private async Task<string> DownloadFileFromUrl(UrlRequest request)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".dwg");

            using (var client = new HttpClient())
            {
                // إضافة headers مخصصة إذا كانت موجودة
                foreach (var header in request.Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                using (var response = await client.GetAsync(request.Url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    // التحقق من نوع الملف
                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    if (contentType != "application/acad" && contentType != "application/octet-stream" &&
                        !request.Url.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception("URL does not point to a valid DWG file");
                    }

                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }
            }

            return tempPath;
        }

    }
}