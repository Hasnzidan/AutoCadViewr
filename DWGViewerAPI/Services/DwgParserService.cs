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
                result.Layers.Add(
                    new DwgLayer
                    {
                        Name = layer.Name,
                        Handle = layer.Handle.ToString(),
                        Color = $"{layer.Color.R}, {layer.Color.G}, {layer.Color.B}",
                        IsVisible = layer.IsOn,
                        IsFrozen = false,
                    }
                );
            }

            // Extract Linetypes
            foreach (var lt in doc.LineTypes)
            {
                var dwgLt = new DwgLinetype { Name = lt.Name, Description = lt.Description };

                // Pattern segments
                foreach (var segment in lt.Segments)
                {
                    // segment.Length is the dash/gap length
                    dwgLt.Pattern.Add(segment.Length);
                }

                result.Linetypes.Add(dwgLt);
            }

            // Extract Entities
            var allStats = new Dictionary<string, int>();
            var convertedStats = new Dictionary<string, int>();
            int totalEntities = 0;
            int convertedCount = 0;

            foreach (var entity in doc.Entities)
            {
                totalEntities++;
                // Get the base class name or specific type for raw stats
                string rawType = entity.GetType().Name;

                // Normalizing names for UI statistics mapping
                if (rawType == "TextEntity")
                    rawType = "Text";
                if (rawType == "LwPolyline")
                    rawType = "Polyline";
                if (rawType == "TableEntity")
                    rawType = "Table";

                if (!allStats.ContainsKey(rawType))
                    allStats[rawType] = 0;
                allStats[rawType]++;

                var dwgEntity = _entityConverter.Convert(entity, doc);
                if (dwgEntity != null)
                {
                    result.Entities.Add(dwgEntity);
                    convertedCount++;

                    // Use rawType for stats to match the 'allStats' key exactly
                    if (!convertedStats.ContainsKey(rawType))
                        convertedStats[rawType] = 0;
                    convertedStats[rawType]++;
                }
            }

            result.Metadata["TotalEntitiesFound"] = totalEntities;
            result.Metadata["TotalEntitiesConverted"] = convertedCount;
            result.Metadata["DetailedStats_AllInFile"] = allStats;
            result.Metadata["DetailedStats_Converted"] = convertedStats;

            Console.WriteLine($"Parsed {convertedCount}/{totalEntities} entities.");

            return result;
        }
    }
}
