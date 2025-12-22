namespace DWGViewerAPI.Models.Geometry
{
    public class LineGeometry
    {
        public double[] StartPoint { get; set; } = new double[3];
        public double[] EndPoint { get; set; } = new double[3];
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
        public double[] Normal { get; set; } = new[] { 0.0, 0.0, 1.0 };
    }

    public class PolylineGeometry
    {
        public List<double[]> Vertices { get; set; } = new();
        public List<double> Bulges { get; set; } = new();
        public bool IsClosed { get; set; }
    }

    public class TextGeometry
    {
        public double[] InsertionPoint { get; set; } = new double[3];
        public string Text { get; set; } = string.Empty;
        public double Height { get; set; }
        public double Rotation { get; set; }
        public double WidthFactor { get; set; }
        public string HorizontalAlignment { get; set; } = string.Empty;
        public string VerticalAlignment { get; set; } = string.Empty;
    }

    public class MTextGeometry
    {
        public double[] InsertionPoint { get; set; } = new double[3];
        public string Text { get; set; } = string.Empty;
        public double Height { get; set; }
        public double Rotation { get; set; }
        public double RectangleWidth { get; set; }
        public string AttachmentPoint { get; set; } = string.Empty;
    }

    public class SplineGeometry
    {
        public List<double[]> ControlPoints { get; set; } = new();
        public List<double[]> FitPoints { get; set; } = new();
        public List<double> Knots { get; set; } = new();
        public int Degree { get; set; }
        public bool IsClosed { get; set; }
        public bool IsPeriodic { get; set; }
        public bool IsRational { get; set; }
    }

    public class EllipseGeometry
    {
        public double[] Center { get; set; } = new double[3];
        public double[] MajorAxis { get; set; } = new double[3];
        public double MinorAxisRatio { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }
    }

    public class HatchGeometry
    {
        public List<List<double[]>> Boundaries { get; set; } = new();
        public string PatternName { get; set; } = string.Empty;
        public string PatternType { get; set; } = string.Empty;
        public bool IsSolid { get; set; }
    }

    public class PointGeometry
    {
        public double[] Location { get; set; } = new double[3];
    }

    public class DimensionLinearGeometry
    {
        public double[] ExtLine1Point { get; set; } = new double[3];
        public double[] ExtLine2Point { get; set; } = new double[3];
        public double[] DimLineLocation { get; set; } = new double[3];
        public double Rotation { get; set; }
        public double Measurement { get; set; }
    }

    public class DimensionAlignedGeometry
    {
        public double[] ExtLine1Point { get; set; } = new double[3];
        public double[] ExtLine2Point { get; set; } = new double[3];
        public double[] DimLineLocation { get; set; } = new double[3];
        public double Measurement { get; set; }
    }

    public class DimensionRadiusGeometry
    {
        public double[] Center { get; set; } = new double[3];
        public double[] ChordPoint { get; set; } = new double[3];
        public double Measurement { get; set; }
    }

    public class DimensionDiameterGeometry
    {
        public double[] ChordPoint { get; set; } = new double[3];
        public double[] FarChordPoint { get; set; } = new double[3];
        public double Measurement { get; set; }
    }

    public class DimensionAngularGeometry
    {
        public double[] CenterPoint { get; set; } = new double[3];
        public double[] FirstPoint { get; set; } = new double[3];
        public double[] SecondPoint { get; set; } = new double[3];
        public double Measurement { get; set; }
    }

    public class InsertGeometry
    {
        public double[] InsertionPoint { get; set; } = new double[3];
        public double[] Origin { get; set; } = new double[3];
        public double[] Scale { get; set; } = new double[3];
        public double Rotation { get; set; }
        public string BlockName { get; set; } = string.Empty;
    }

    public class SolidGeometry
    {
        public List<double[]> Vertices { get; set; } = new();
    }

    public class Face3DGeometry
    {
        public List<double[]> Vertices { get; set; } = new();
        public object? EdgeFlags { get; set; }
    }

    public class LeaderGeometry
    {
        public List<double[]> Vertices { get; set; } = new();
        public bool HasArrowhead { get; set; }
        public bool HasHookline { get; set; }
    }

    public class MultiLeaderGeometry
    {
        public double[] LandingLocation { get; set; } = new double[3];
        public double DoglegLength { get; set; }
    }

    public class ViewportGeometry
    {
        public double[] Center { get; set; } = new double[3];
        public double Width { get; set; }
        public double Height { get; set; }
        public double[] ViewCenter { get; set; } = new double[2];
        public double ViewHeight { get; set; }
        public double Scale { get; set; }
    }
}
