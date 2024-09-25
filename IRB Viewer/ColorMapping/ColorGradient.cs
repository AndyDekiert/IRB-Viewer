namespace IRB_Viewer.ColorMapping;

public class ColorGradient {
    private readonly List<ColorStop> stops;

    public readonly double Min;
    public readonly double Max;
    
    public class ColorStop {
        public readonly double Value;
        public readonly Color Color;

        public ColorStop(double value, Color color) {
            Value = value;
            Color = color;
        }
    }

    public ColorGradient(IEnumerable<ColorStop> stops) {
        this.stops = stops.ToList();
        if (this.stops.Count < 2) throw new Exception("Invalid color gradient. At least two color stops are required.");
        
        this.stops.Sort((o1, o2) => o1.Value.CompareTo(o2.Value));
        Min = this.stops.First().Value;
        Max = this.stops.Last().Value;
    }

    public Color GetColor(double value) {
        value = Math.Max(Min, Math.Min(Max, value));

        ColorStop? begin = null;
        ColorStop? end = null;
        for (int i = 0; i < stops.Count; i++) {
            ColorStop stop = stops[i];
            if (stop.Value >= value) {
                begin = stops[Math.Max(0, i - 1)];
                end = stop;
                break;
            }
        }

        if (begin == null || end == null) return Colors.Black;

        int alpha = (int) Math.Round(255 * InterpolateLinear(
            begin.Value, begin.Color.Alpha, end.Value, end.Color.Alpha, value));
        int red = (int) Math.Round(255 * InterpolateLinear(
            begin.Value, begin.Color.Red, end.Value, end.Color.Red, value));
        int green = (int) Math.Round(255 * InterpolateLinear(
            begin.Value, begin.Color.Green, end.Value, end.Color.Green, value));
        int blue = (int) Math.Round(255 * InterpolateLinear(
            begin.Value, begin.Color.Blue, end.Value, end.Color.Blue, value));
        return Color.FromRgba(red, green, blue, alpha);
    }

    private static double InterpolateLinear(double x1, double y1, double x2, double y2, double xInterpol) {
        // Check for invalid input
        if (y1 == y2) return y1;

        // Linear equation from p1(x1,y1) to p2(x2,y2). Solve for yInterpol.
        // with m = (p2.y - p1.y) / (p2.x - p1.x);
        return (xInterpol - x1) * (y2 - y1) / (x2 - x1) + y1;
    }
}