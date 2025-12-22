using ACadSharp;
using ACadSharp.Entities;

namespace DWGViewerAPI.Services
{
    public class ColorResolver
    {
        public ACadSharp.Color Resolve(Entity entity, CadDocument doc)
        {
            var color = entity.Color;

            if (color.IsByLayer)
            {
                return entity.Layer?.Color ?? new ACadSharp.Color(255, 255, 255);
            }
            else if (color.IsByBlock)
            {
                // Simple default for now
                return new ACadSharp.Color(255, 255, 255);
            }

            return color;
        }

        public string GetColorMethodName(ACadSharp.Color color)
        {
            if (color.IsByLayer)
                return "ByLayer";
            if (color.IsByBlock)
                return "ByBlock";
            return "Direct";
        }
    }
}
