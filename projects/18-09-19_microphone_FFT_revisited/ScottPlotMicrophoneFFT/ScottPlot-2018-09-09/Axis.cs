using System;
using System.Collections.Generic;

namespace ScottPlot
{
    public class Axis
    {
        public double Min { get; set; }

        public double Max { get; set; }

        public int PxSize { get; set; }

        public bool Inverted { get; }

        public double UnitsPerPx { get; private set; }

        public double PxPerUnit { get; set; }

        public double Span => Max - Min;

        public double Center => (Max + Min) / 2.0;

        /// <summary>
        /// Pre-prepare recommended major and minor ticks
        /// </summary>
        public Tick[] TicksMajor { get; private set; }

        public Tick[] TicksMinor { get; private set; }

        private const double PixelsPerTick = 70;

        /// <summary>
        /// Single-dimensional axis (i.e., x-axis)
        /// </summary>
        /// <param name="min">lower bound (units)</param>
        /// <param name="max">upper bound (units)</param>
        /// <param name="sizePx">size of this axis (pixels)</param>
        /// <param name="inverted">inverted axis vs. pixel position (common for Y-axis)</param>
        public Axis(double min, double max, int sizePx = 500, bool inverted = false)
        {
            Min = min;
            Max = max;
            Inverted = inverted;
            Resize(sizePx);
        }

        /// <summary>
        /// Tell the Axis how large it will be on the screen
        /// </summary>
        /// <param name="sizePx">size of this axis (pixels)</param>
        public void Resize(int sizePx)
        {
            PxSize = sizePx;
            RecalculateScale();
        }

        /// <summary>
        /// Update units/pixels conversion scales.
        /// </summary>
        public void RecalculateScale()
        {
            PxPerUnit = PxSize / (Max - Min);
            UnitsPerPx = (Max - Min) / PxSize;
            RecalculateTicks();
        }

        /// <summary>
        /// Zoom in on the center of Axis by a fraction.
        /// A fraction of 2 means that the new width will be 1/2 as wide as the old width.
        /// A fraction of 0.1 means the new width will show 10 times more axis length.
        /// </summary>
        /// <param name="zoomFrac">Fractional amount to zoom</param>
        public void Zoom(double zoomFrac)
        {
            double newSpan = Span / zoomFrac;
            double newCenter = Center;
            Min = newCenter - newSpan / 2;
            Max = newCenter + newSpan / 2;
            RecalculateScale();
        }

        /// <summary>
        /// Given a position on the axis (in units), return its position on the screen (in pixels).
        /// Returned values may be negative, or greater than the pixel width.
        /// </summary>
        /// <param name="unit">position (units)</param>
        /// <returns></returns>
        public int GetPixel(double unit)
        {
            var px = (int)((unit - Min) * PxPerUnit);
            if (Inverted)
            {
                px = PxSize - px;
            }

            return px;
        }

        /// <summary>
        /// Given an arbitrary number, return the nearerest round number
        /// (i.e., 1000, 500, 100, 50, 10, 5, 1, .5, .1, .05, .01)
        /// </summary>
        /// <param name="target">the number to approximate</param>
        /// <returns></returns>
        private static double RoundNumberNear(double target)
        {
            target = Math.Abs(target);
            var lastDivision = 2;
            double round = 1000000000000;
            while (round > 0.00000000001)
            {
                if (round <= target)
                {
                    return round;
                }

                round /= lastDivision;
                lastDivision = lastDivision == 2 ? 5 : 2;
            }

            return 0;
        }

        /// <summary>
        /// Return an array of tick objects given a custom target tick count
        /// </summary>
        private Tick[] GenerateTicks(int targetTickCount)
        {
            if (targetTickCount <= 0)
            {
                return Array.Empty<Tick>();
            }

            var ticks = new List<Tick>();

            double tickSize = RoundNumberNear((Max - Min) / targetTickCount * 1.5);
            var lastTick = 123456789;
            for (var i = 0; i < PxSize; i++)
            {
                double thisPosition = i * UnitsPerPx + Min;
                var thisTick = (int)(thisPosition / tickSize);

                if (thisTick == lastTick)
                {
                    continue;
                }

                lastTick = thisTick;
                double thisPositionRounded = (int)(thisPosition / tickSize) * tickSize;
                if (thisPositionRounded > Min && thisPositionRounded < Max)
                {
                    ticks.Add(new Tick(thisPositionRounded, GetPixel(thisPositionRounded), Max - Min));
                }
            }

            return ticks.ToArray();
        }

        private void RecalculateTicks()
        {
            double tickDensityX = PxSize / PixelsPerTick; // approx. 1 tick per this many pixels
            TicksMajor = GenerateTicks((int)(tickDensityX * 5)); // relative density of minor to major ticks
            TicksMinor = GenerateTicks((int)(tickDensityX * 1));
        }
    }
}