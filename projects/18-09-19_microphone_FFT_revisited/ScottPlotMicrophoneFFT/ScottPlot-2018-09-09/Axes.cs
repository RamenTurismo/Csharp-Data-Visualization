using System;

namespace ScottPlot
{
    /// <summary>
    /// The MouseAxis class simplifies adjusting axis edges for click-and-drag pan and zoom events.
    /// After being instantiated with an initial axis and mouse position, it can return axis limits
    /// for panning or zooming given a new mouse position later.
    /// </summary>
    internal class MouseAxis
    {
        private readonly Axis xAxStart;
        private readonly Axis yAxStart;
        private readonly int xMouseStart;
        private readonly int yMouseStart;

        public double X1 { get; private set; }

        public double X2 { get; private set; }

        public double Y1 { get; private set; }

        public double Y2 { get; private set; }

        public MouseAxis(Axis xAxis, Axis yAxis, int mouseX, int mouseY)
        {
            xAxStart = new Axis(xAxis.Min, xAxis.Max, xAxis.PxSize, xAxis.Inverted);
            yAxStart = new Axis(yAxis.Min, yAxis.Max, yAxis.PxSize, yAxis.Inverted);
            xMouseStart = mouseX;
            yMouseStart = mouseY;
            Pan(0, 0);
        }

        public void Pan(int xMouseNow, int yMouseNow)
        {
            int dX = xMouseStart - xMouseNow;
            int dY = yMouseNow - yMouseStart;
            X1 = xAxStart.Min + dX * xAxStart.UnitsPerPx;
            X2 = xAxStart.Max + dX * xAxStart.UnitsPerPx;
            Y1 = yAxStart.Min + dY * yAxStart.UnitsPerPx;
            Y2 = yAxStart.Max + dY * yAxStart.UnitsPerPx;
        }

        public void Zoom(int xMouseNow, int yMouseNow)
        {
            double dX = (xMouseNow - xMouseStart) * xAxStart.UnitsPerPx;
            double dY = (yMouseStart - yMouseNow) * yAxStart.UnitsPerPx;

            double dXFrac = dX / (Math.Abs(dX) + xAxStart.Span);
            double dYFrac = dY / (Math.Abs(dY) + yAxStart.Span);

            double xNewSpan = xAxStart.Span / Math.Pow(10, dXFrac);
            double yNewSpan = yAxStart.Span / Math.Pow(10, dYFrac);

            double xNewCenter = xAxStart.Center;
            double yNewCenter = yAxStart.Center;

            X1 = xNewCenter - xNewSpan / 2;
            X2 = xNewCenter + xNewSpan / 2;

            Y1 = yNewCenter - yNewSpan / 2;
            Y2 = yNewCenter + yNewSpan / 2;
        }
    }
}