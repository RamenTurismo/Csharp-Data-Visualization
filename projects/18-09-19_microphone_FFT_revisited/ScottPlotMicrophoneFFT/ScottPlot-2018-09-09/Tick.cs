namespace ScottPlot
{
    /// <summary>
    /// The Tick object stores details about a single tick and can generate relevant labels.
    /// </summary>
    public class Tick
    {
        public double PosUnit { get; }

        public int PosPixel { get; }

        public double SpanUnits { get; }

        public Tick(double value, int pixel, double axisSpan)
        {
            PosUnit = value;
            PosPixel = pixel;
            SpanUnits = axisSpan;
        }

        public string Label
        {
            get
            {
                if (SpanUnits < .01)
                {
                    return $"{PosUnit:0.0000}";
                }

                if (SpanUnits < .1)
                {
                    return $"{PosUnit:0.000}";
                }

                if (SpanUnits < 1)
                {
                    return $"{PosUnit:0.00}";
                }

                if (SpanUnits < 10)
                {
                    return $"{PosUnit:0.0}";
                }

                return $"{PosUnit:0}";
            }
        }
    }
}