using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class ViewportConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Viewport;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var viewport = (Viewport)entity;
            result.Type = "Viewport";
            
            result.Geometry = new ViewportGeometry
            {
                Center = new[] { viewport.Center.X, viewport.Center.Y, viewport.Center.Z },
                Width = viewport.Width,
                Height = viewport.Height,
                ViewCenter = new[] { viewport.ViewCenter.X, viewport.ViewCenter.Y },
                ViewHeight = viewport.ViewHeight,
                Scale = viewport.ViewHeight > 0 ? viewport.Height / viewport.ViewHeight : 1.0
            };

            result.DwgProperties.Add("Center", $"{viewport.Center.X:F2}, {viewport.Center.Y:F2}");
            result.DwgProperties.Add("Width", viewport.Width);
            result.DwgProperties.Add("Height", viewport.Height);
            result.DwgProperties.Add("ViewCenter", $"{viewport.ViewCenter.X:F2}, {viewport.ViewCenter.Y:F2}");
            result.DwgProperties.Add("ViewHeight", viewport.ViewHeight);
            result.DwgProperties.Add("Scale", viewport.ViewHeight > 0 ? viewport.Height / viewport.ViewHeight : 1.0);
            try {
                dynamic dvp = viewport;
                result.DwgProperties.Add("IsOn", dvp.Status != 0);
            } catch {
                result.DwgProperties.Add("IsOn", true);
            }
        }
    }
}