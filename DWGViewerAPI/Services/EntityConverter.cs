using ACadSharp;
using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services
{
    public class EntityConverter : IEntityConverter
    {
        private readonly ColorResolver _colorResolver;
        private readonly IEnumerable<IEntityTypeConverter> _typeConverters;

        public EntityConverter(ColorResolver colorResolver, IEnumerable<IEntityTypeConverter> typeConverters)
        {
            _colorResolver = colorResolver;
            _typeConverters = typeConverters;
        }

        public DwgEntity? Convert(Entity entity, CadDocument doc)
        {
            var converter = _typeConverters.FirstOrDefault(c => c.CanConvert(entity));
            if (converter == null) return null; // نوع غير مدعوم حالياً

            var actualColor = _colorResolver.Resolve(entity, doc);

            var dwgEntity = new DwgEntity
            {
                Id = entity.Handle.ToString(),
                DwgProperties = new Dictionary<string, object>
                {
                    { "Handle", entity.Handle.ToString() },
                    { "ObjectName", entity.ObjectName },
                    { "ObjectType", entity.GetType().Name },
                    { "OwnerHandle", entity.Owner?.Handle.ToString() ?? "0" },
                    { "Layer", entity.Layer?.Name ?? "0" },
                    { "LayerHandle", entity.Layer?.Handle.ToString() ?? "0" },
                    { "Color", $"{actualColor.R}, {actualColor.G}, {actualColor.B}" },
                    { "ColorIndex", entity.Color.Index },
                    { "ColorMethod", _colorResolver.GetColorMethodName(entity.Color) },
                    { "IsByLayer", entity.Color.IsByLayer },
                    { "IsByBlock", entity.Color.IsByBlock },
                    { "LineType", entity.LineType?.Name ?? "Continuous" },
                    { "LineWeight", entity.LineWeight.ToString() },
                    { "IsInvisible", entity.IsInvisible },
                    { "Transparency", entity.Transparency.ToString() }
                }
            };

            // استدعاء المحول الخاص بالنوع
            converter.Convert(entity, dwgEntity, doc);

            return dwgEntity;
        }
    }
}
