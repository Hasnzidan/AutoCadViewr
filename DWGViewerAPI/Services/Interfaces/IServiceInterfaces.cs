using ACadSharp;
using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;

namespace DWGViewerAPI.Services.Interfaces
{
    public interface IDwgParserService
    {
        List<DwgEntity> ParseDwgFile(string filePath);
    }

    public interface IDwgReaderService
    {
        CadDocument ReadDwg(string filePath);
    }

    public interface IEntityConverter
    {
        DwgEntity? Convert(Entity entity, CadDocument doc);
    }
}
