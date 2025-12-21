using ACadSharp.Entities;
using DWGViewerAPI.Models.Entities;
using DWGViewerAPI.Models.Geometry;
using DWGViewerAPI.Services.Interfaces;

namespace DWGViewerAPI.Services.Converters
{
    public class TextConverter : IEntityTypeConverter
    {
        public bool CanConvert(Entity entity) => entity is TextEntity || entity is MText;

        public void Convert(Entity entity, DwgEntity result, ACadSharp.CadDocument doc)
        {
            if (entity is TextEntity text)
            {
                result.Type = "Text";
                result.Geometry = new TextGeometry
                {
                    Position = new[] { text.InsertPoint.X, text.InsertPoint.Y, text.InsertPoint.Z },
                    Content = text.Value,
                    Rotation = text.Rotation,
                    Height = text.Height
                };

                result.DwgProperties.Add("Value", text.Value);
                result.DwgProperties.Add("Height", text.Height);
                result.DwgProperties.Add("Rotation", text.Rotation);
                result.DwgProperties.Add("HorizontalAlignment", text.HorizontalAlignment.ToString());
                result.DwgProperties.Add("VerticalAlignment", text.VerticalAlignment.ToString());
            }
            else if (entity is MText mtext)
            {
                result.Type = "MText";
                result.Geometry = new TextGeometry
                {
                    Position = new[] { mtext.InsertPoint.X, mtext.InsertPoint.Y, mtext.InsertPoint.Z },
                    Content = CleanMText(mtext.Value),
                    Rotation = mtext.Rotation,
                    Height = mtext.Height
                };

                result.DwgProperties.Add("Value", CleanMText(mtext.Value));
                result.DwgProperties.Add("RawValue", mtext.Value);
                result.DwgProperties.Add("Height", mtext.Height);
                result.DwgProperties.Add("Rotation", mtext.Rotation);
                result.DwgProperties.Add("AttachmentPoint", mtext.AttachmentPoint.ToString());
            }
        }

        private string CleanMText(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            
            // Remove formatting like {\fArial|b0|i0|c0|p34;Text}
            // Simple regex approach to remove anything between { } that starts with \ and contains ;
            var cleaned = System.Text.RegularExpressions.Regex.Replace(value, @"\{[^\}]*\}", m => {
                var content = m.Value.Substring(1, m.Value.Length - 2);
                var semiIndex = content.LastIndexOf(';');
                return semiIndex >= 0 ? content.Substring(semiIndex + 1) : content;
            });

            // Remove other common codes like \P (newline), \L (underline), etc.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\\[PLOT].", "");
            cleaned = cleaned.Replace("\\P", "\n");
            
            return cleaned;
        }
    }
}
