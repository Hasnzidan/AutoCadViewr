using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class SolidConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is Solid || entity is Face3D;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            if (entity is Solid solid)
            {
                result.Type = "Solid";
                result.Geometry = new SolidGeometry
                {
                    Vertices = new List<double[]>
                    {
                        new[] { solid.FirstCorner.X, solid.FirstCorner.Y, solid.FirstCorner.Z },
                        new[] { solid.SecondCorner.X, solid.SecondCorner.Y, solid.SecondCorner.Z },
                        new[] { solid.ThirdCorner.X, solid.ThirdCorner.Y, solid.ThirdCorner.Z },
                        new[] { solid.FourthCorner.X, solid.FourthCorner.Y, solid.FourthCorner.Z }
                    }
                };

                result.DwgProperties.Add("Thickness", solid.Thickness);
            }
            else if (entity is Face3D face)
            {
                result.Type = "3DFace";
                result.Geometry = new Face3DGeometry
                {
                    Vertices = new List<double[]>
                    {
                        new[] { face.FirstCorner.X, face.FirstCorner.Y, face.FirstCorner.Z },
                        new[] { face.SecondCorner.X, face.SecondCorner.Y, face.SecondCorner.Z },
                        new[] { face.ThirdCorner.X, face.ThirdCorner.Y, face.ThirdCorner.Z },
                        new[] { face.FourthCorner.X, face.FourthCorner.Y, face.FourthCorner.Z }
                    },
                    EdgeFlags = face.Flags
                };

                result.DwgProperties.Add("EdgeFlags", face.Flags.ToString());
            }
        }
    }
}