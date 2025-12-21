using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class DimensionConverter : IEntityTypeConverter
    {
        private readonly IServiceProvider _serviceProvider;

        public DimensionConverter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool CanConvert(Entity entity) => entity is Dimension;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            var dim = (Dimension)entity;
            result.Type = dim.GetType().Name;
            
            if (dim is DimensionLinear linear)
            {
                result.Geometry = new DimensionLinearGeometry
                {
                    ExtLine1Point = new[] { linear.FirstPoint.X, linear.FirstPoint.Y, linear.FirstPoint.Z },
                    ExtLine2Point = new[] { linear.SecondPoint.X, linear.SecondPoint.Y, linear.SecondPoint.Z },
                    DimLineLocation = new[] { linear.DefinitionPoint.X, linear.DefinitionPoint.Y, linear.DefinitionPoint.Z },
                    Rotation = linear.Rotation,
                    Measurement = linear.Measurement
                };
                
                result.DwgProperties.Add("FirstPoint", $"{linear.FirstPoint.X:F2}, {linear.FirstPoint.Y:F2}");
                result.DwgProperties.Add("SecondPoint", $"{linear.SecondPoint.X:F2}, {linear.SecondPoint.Y:F2}");
                result.DwgProperties.Add("Measurement", linear.Measurement);
                result.DwgProperties.Add("Rotation", linear.Rotation * (180 / Math.PI));
            }
            else if (dim is DimensionAligned aligned)
            {
                result.Geometry = new DimensionAlignedGeometry
                {
                    ExtLine1Point = new[] { aligned.FirstPoint.X, aligned.FirstPoint.Y, aligned.FirstPoint.Z },
                    ExtLine2Point = new[] { aligned.SecondPoint.X, aligned.SecondPoint.Y, aligned.SecondPoint.Z },
                    DimLineLocation = new[] { aligned.DefinitionPoint.X, aligned.DefinitionPoint.Y, aligned.DefinitionPoint.Z },
                    Measurement = aligned.Measurement
                };
                
                result.DwgProperties.Add("FirstPoint", $"{aligned.FirstPoint.X:F2}, {aligned.FirstPoint.Y:F2}");
                result.DwgProperties.Add("SecondPoint", $"{aligned.SecondPoint.X:F2}, {aligned.SecondPoint.Y:F2}");
                result.DwgProperties.Add("Measurement", aligned.Measurement);
            }
            else if (dim is DimensionRadius radius)
            {
                result.Geometry = new DimensionRadiusGeometry
                {
                    Center = new[] { radius.AngleVertex.X, radius.AngleVertex.Y, radius.AngleVertex.Z },
                    ChordPoint = new[] { radius.DefinitionPoint.X, radius.DefinitionPoint.Y, radius.DefinitionPoint.Z },
                    Measurement = radius.Measurement
                };
                
                result.DwgProperties.Add("Center", $"{radius.AngleVertex.X:F2}, {radius.AngleVertex.Y:F2}");
                result.DwgProperties.Add("Measurement", radius.Measurement);
            }
            else if (dim is DimensionDiameter diameter)
            {
                result.Geometry = new DimensionDiameterGeometry
                {
                    ChordPoint = new[] { diameter.AngleVertex.X, diameter.AngleVertex.Y, diameter.AngleVertex.Z },
                    FarChordPoint = new[] { diameter.DefinitionPoint.X, diameter.DefinitionPoint.Y, diameter.DefinitionPoint.Z },
                    Measurement = diameter.Measurement
                };
                
                result.DwgProperties.Add("Measurement", diameter.Measurement);
            }
            else if (dim is DimensionAngular3Pt angular)
            {
                result.Geometry = new DimensionAngularGeometry
                {
                    CenterPoint = new[] { angular.AngleVertex.X, angular.AngleVertex.Y, angular.AngleVertex.Z },
                    FirstPoint = new[] { angular.FirstPoint.X, angular.FirstPoint.Y, angular.FirstPoint.Z },
                    SecondPoint = new[] { angular.SecondPoint.X, angular.SecondPoint.Y, angular.SecondPoint.Z },
                    Measurement = angular.Measurement
                };
                
                result.DwgProperties.Add("Measurement", angular.Measurement);
            }

            result.DwgProperties.Add("DimensionStyle", dim.Style?.Name ?? "Standard");
            result.DwgProperties.Add("Text", dim.Text ?? "");
            result.DwgProperties.Add("TextRotation", dim.TextRotation * (180 / Math.PI));

            // Recursive conversion of dimension block entities (the actual lines/texts)
            if (dim.Block != null)
            {
                var rootConverter = (IEntityConverter)_serviceProvider.GetService(typeof(IEntityConverter));
                if (rootConverter != null)
                {
                    foreach (var childEntity in dim.Block.Entities)
                    {
                        var converted = rootConverter.Convert(childEntity, doc);
                        if (converted != null)
                        {
                            result.Entities.Add(converted);
                        }
                    }
                }
            }
        }
    }
}