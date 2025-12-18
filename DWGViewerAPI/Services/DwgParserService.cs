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

        public List<DwgEntity> ParseDwgFile(string filePath)
        {
            var entities = new List<DwgEntity>();
            var doc = _readerService.ReadDwg(filePath);

            foreach (var entity in doc.Entities)
            {
                var dwgEntity = _entityConverter.Convert(entity, doc);
                if (dwgEntity != null)
                {
                    entities.Add(dwgEntity);
                }
            }

            return entities;
        }
    }
}