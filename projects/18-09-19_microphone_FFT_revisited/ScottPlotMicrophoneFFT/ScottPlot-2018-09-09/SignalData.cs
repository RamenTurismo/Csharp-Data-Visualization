using System.Drawing;

namespace ScottPlot
{
    internal class SignalData
    {
        public double[] Values { get; }

        public double XSpacing { get; }

        public double OffsetX { get; }

        public double OffsetY { get; }

        public float LineWidth { get; }

        public Color LineColor { get; }

        public SignalData(double[] values, double sampleRate, double offsetX = 0, double offsetY = 0, Color? lineColor = null, float lineWidth = 1)
        {
            Values = values;
            XSpacing = 1.0 / sampleRate;
            OffsetX = offsetX;
            OffsetY = offsetY;

            if (lineColor == null)
            {
                lineColor = Color.Red;
            }

            LineColor = (Color)lineColor;
            LineWidth = lineWidth;
        }
    }
}