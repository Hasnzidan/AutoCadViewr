using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;

namespace DWGViewerAPI.Services.Interfaces
{
    public interface IEntityTypeConverter
    {
        bool CanConvert(Entity entity);
        void Convert(Entity entity, DwgEntity result);
    }
}
