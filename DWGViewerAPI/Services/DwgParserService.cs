using DWGViewerAPI.Models;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services
{
    public class DwgParserService : IDwgParserService
    {
        private readonly IDwgReaderService _readerService;
        private readonly IEntityConverter _entityConverter;

        public DwgParserService(IDwgReaderService readerService, IEntityConverter entityConverter)
        {
            _readerService = readerService;
            _entityConverter = entityConverter;
        }

        public DwgFileResult ParseDwgFile(string filePath)
        {
            var result = new DwgFileResult();
            var doc = _readerService.ReadDwg(filePath);

            // Extract Layers
            foreach (var layer in doc.Layers)
            {
                result.Layers.Add(new DwgLayer
                {
                    Name = layer.Name,
                    Handle = layer.Handle.ToString(),
                    Color = $"{layer.Color.R}, {layer.Color.G}, {layer.Color.B}",
                    IsVisible = layer.IsOn,
                    IsFrozen = false 
                });
            }

            // Extract Linetypes
            foreach (var lt in doc.LineTypes)
            {
                var dwgLt = new DwgLinetype
                {
                    Name = lt.Name,
                    Description = lt.Description
                };

                // Pattern segments
                foreach (var segment in lt.Segments)
                {
                    // segment.Length is the dash/gap length
                    dwgLt.Pattern.Add(segment.Length);
                }

                result.Linetypes.Add(dwgLt);
            }

            // Extract Entities
            foreach (var entity in doc.Entities)
            {
                var dwgEntity = _entityConverter.Convert(entity, doc);
                if (dwgEntity != null)
                {
                    result.Entities.Add(dwgEntity);
                }
            }

            return result;
        }
    }
}