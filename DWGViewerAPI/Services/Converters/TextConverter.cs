using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;
using System.Text.RegularExpressions;

namespace DWGViewerAPI.Services.Converters
{
    public class TextConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is TextEntity || entity is MText;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            if (entity is TextEntity text)
            {
                var cleanedText = CleanText(text.Value);
                
                result.Type = "Text";
                result.Geometry = new TextGeometry
                {
                    InsertionPoint = new[]
                    {
                        text.InsertPoint.X,
                        text.InsertPoint.Y,
                        text.InsertPoint.Z,
                    },
                    Text = cleanedText,
                    Height = text.Height,
                    Rotation = text.Rotation,
                    WidthFactor = text.WidthFactor,
                    HorizontalAlignment = text.HorizontalAlignment.ToString(),
                    VerticalAlignment = text.VerticalAlignment.ToString(),
                };

                result.DwgProperties.Add("Text", cleanedText);
                result.DwgProperties.Add("Height", text.Height);
                result.DwgProperties.Add("Rotation", text.Rotation * (180 / Math.PI));
                result.DwgProperties.Add("WidthFactor", text.WidthFactor);
                result.DwgProperties.Add("Style", text.Style?.Name ?? "Standard");
                result.DwgProperties.Add(
                    "HorizontalAlignment",
                    text.HorizontalAlignment.ToString()
                );
                result.DwgProperties.Add("VerticalAlignment", text.VerticalAlignment.ToString());
            }
            else if (entity is MText mtext)
            {
                var cleanedText = CleanText(mtext.Value);
                
                result.Type = "MText";
                result.Geometry = new MTextGeometry
                {
                    InsertionPoint = new[]
                    {
                        mtext.InsertPoint.X,
                        mtext.InsertPoint.Y,
                        mtext.InsertPoint.Z,
                    },
                    Text = cleanedText,
                    Height = mtext.Height,
                    Rotation = mtext.Rotation,
                    RectangleWidth = mtext.RectangleWidth,
                    AttachmentPoint = mtext.AttachmentPoint.ToString(),
                };

                result.DwgProperties.Add("Text", cleanedText);
                result.DwgProperties.Add("Height", mtext.Height);
                result.DwgProperties.Add("Rotation", mtext.Rotation * (180 / Math.PI));
                result.DwgProperties.Add("RectangleWidth", mtext.RectangleWidth);
                result.DwgProperties.Add("Style", mtext.Style?.Name ?? "Standard");
                result.DwgProperties.Add("AttachmentPoint", mtext.AttachmentPoint.ToString());
            }
        }

        /// <summary>
        /// Cleans AutoCAD text by removing formatting codes and control characters
        /// </summary>
        private string CleanText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Remove MText formatting codes like \P (paragraph), \X (formatting), etc.
            var cleaned = Regex.Replace(value, @"\\[A-Za-z]\d*;?", "");
            
            // Remove curly braces used for formatting groups
            cleaned = cleaned.Replace("{", "").Replace("}", "");
            
            // Replace \P with newline for paragraph breaks
            cleaned = value.Replace("\\P", "\n").Replace("\\p", "\n");
            
            // Remove other common formatting codes
            cleaned = Regex.Replace(cleaned, @"\\[A-Za-z]\d*;?", "");
            cleaned = cleaned.Replace("{", "").Replace("}", "");
            
            // Trim whitespace
            cleaned = cleaned.Trim();

            return cleaned;
        }
    }
}