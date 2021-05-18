using System.Drawing;

namespace ScottPlot
{
    internal class AxisLine
    {
        public double Value { get; }

        public float LineWidth { get; }

        public Color LineColor { get; }

        public AxisLine(double ypos, float lineWidth, Color lineColor)
        {
            Value = ypos;
            LineWidth = lineWidth;
            LineColor = lineColor;
        }
    }
}