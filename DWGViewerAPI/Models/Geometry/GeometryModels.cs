namespace DWGViewerAPI.Models.Geometry
{
    public class LineGeometry
    {
        public List<double[]> Points { get; set; } = new();
    }

    public class CircleGeometry
    {
        public double[] Center { get; set; } = new double[3];
        public double Radius { get; set; }
    }

    public class ArcGeometry
    {
        public double[] Center { get; set; } = new double[3];
        public double Radius { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }
    }

    public class TextGeometry
    {
        public double[] Position { get; set; } = new double[3];
        public string Content { get; set; } = string.Empty;
    }
}
