using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class InsertConverter : IEntityTypeConverter
    {
        private readonly IServiceProvider _serviceProvider;

        public InsertConverter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool CanConvert(Entity entity) => entity is Insert;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var insert = (Insert)entity;
            result.Type = "Insert";

            result.Geometry = new InsertGeometry
            {
                InsertionPoint = [insert.InsertPoint.X, insert.InsertPoint.Y, insert.InsertPoint.Z],
                Origin = GetBlockOrigin(insert.Block),
                Scale = [insert.XScale, insert.YScale, insert.ZScale],
                Rotation = insert.Rotation,
                BlockName = insert.Block?.Name ?? "",
            };

            // نكتفي بإضافة الخصائص غير الموجودة في المحول الأساسي لتجنب الـ Conflict
            result.DwgProperties["BlockName"] = insert.Block?.Name ?? "";
            result.DwgProperties["ScaleFactor"] =
                $"{insert.XScale}, {insert.YScale}, {insert.ZScale}";
            result.DwgProperties["RotationAngle"] = insert.Rotation * (180 / Math.PI);

            // Recursive conversion
            if (insert.Block != null)
            {
                var rootConverter =
                    _serviceProvider.GetService(typeof(IEntityConverter)) as IEntityConverter;
                if (rootConverter != null)
                {
                    foreach (var childEntity in insert.Block.Entities)
                    {
                        var converted = rootConverter.Convert(childEntity, doc);
                        if (converted != null)
                        {
                            result.Entities.Add(converted);
                        }
                    }
                }
            }

            if (insert.Attributes != null && insert.Attributes.Any())
            {
                var attributes = new Dictionary<string, string>();
                foreach (var attr in insert.Attributes)
                {
                    attributes[attr.Tag] = attr.Value;
                }
                result.DwgProperties["Attributes"] = attributes;
            }
        }

        private double[] GetBlockOrigin(object block)
        {
            if (block == null)
                return [0.0, 0.0, 0.0];
            dynamic dynamicBlock = block;
            try
            {
                return
                [
                    (double)dynamicBlock.Origin.X,
                    (double)dynamicBlock.Origin.Y,
                    (double)dynamicBlock.Origin.Z,
                ];
            }
            catch { }
            try
            {
                return
                [
                    (double)dynamicBlock.BasePoint.X,
                    (double)dynamicBlock.BasePoint.Y,
                    (double)dynamicBlock.BasePoint.Z,
                ];
            }
            catch { }
            return [0.0, 0.0, 0.0];
        }
    }
}
