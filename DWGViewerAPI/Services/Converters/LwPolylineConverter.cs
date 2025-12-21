using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class LwPolylineConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is LwPolyline;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var polyline = (LwPolyline)entity;
            result.Type = "LwPolyline";
            
            var geometry = new LwPolylineGeometry
            {
                IsClosed = polyline.IsClosed
            };

            foreach (var vertex in polyline.Vertices)
            {
                // LwPolyline vertices are 2D (X, Y), we add the elevation (Z) to make it 3D
                geometry.Points.Add(new[] { vertex.Location.X, vertex.Location.Y, polyline.Elevation });
            }

            result.Geometry = geometry;

            result.DwgProperties.Add("Closed", polyline.IsClosed);
            result.DwgProperties.Add("Constant Width", polyline.ConstantWidth);
            result.DwgProperties.Add("Thickness", polyline.Thickness);
            result.DwgProperties.Add("Vertices Count", polyline.Vertices.Count);
        }
    }
}
