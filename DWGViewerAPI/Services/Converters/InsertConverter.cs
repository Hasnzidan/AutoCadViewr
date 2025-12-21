using ACadSharp;
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

        public void Convert(Entity entity, DwgEntity result, CadDocument doc)
        {
            var insert = (Insert)entity;
            result.Type = "Insert";
            
            result.Geometry = new InsertGeometry
            {
                Position = new[] { insert.InsertPoint.X, insert.InsertPoint.Y, insert.InsertPoint.Z },
                Scale = new[] { insert.XScale, insert.YScale, insert.ZScale },
                Rotation = insert.Rotation,
                BlockName = insert.Block.Name
            };

            result.DwgProperties.Add("Block Name", insert.Block.Name);
            result.DwgProperties.Add("Rotation", insert.Rotation);
            result.DwgProperties.Add("Scale", $"{insert.XScale}, {insert.YScale}, {insert.ZScale}");

            // Resolve EntityConverter locally to avoid circular dependency
            var entityConverter = (IEntityConverter)_serviceProvider.GetService(typeof(IEntityConverter))!;

            // Convert entities inside the block
            foreach (var blockEntity in insert.Block.Entities)
            {
                var subEntity = entityConverter.Convert(blockEntity, doc);
                if (subEntity != null)
                {
                    result.Entities.Add(subEntity);
                }
            }
        }
    }
}
