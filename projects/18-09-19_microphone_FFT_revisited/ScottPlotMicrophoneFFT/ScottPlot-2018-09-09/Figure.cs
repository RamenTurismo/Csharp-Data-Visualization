using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;

/* ScottPlot is a class library intended to make it easy to graph large datasets in high speed.
 * 
 * Although features like mouse click-and-drag to zoom and pan are included for easy interactive GUI
 * integration, ScottPlot can be run entirely within console applications as well.
 * 
 * KEY TERMS:
 *      Figure - a Figure object is mostly what the user will interact with. It contains a Frame and a Graph.
 *      Frame - the frame is everything behind the data (the axis labels, grid lines, tick marks, etc).
 *      Graph - the part of the frame which gets drawn on when graphs are plotted.
 *      Axis - information about a single dimension (X vs Y) including the current min/max and pixel scaling.
 *  
 * THEORY OF OPERATION / USE OVERVIEW:
 *      * Create a Figure (telling it the size of the image)
 *      * Resize() can change the size in the future
 *      * Set colors as desired
 *      * Set the axis labels and title as desired
 *      * Zoom() and Pan() can be used to adjust window
 *      * Adjust the axis limits to window the data you wish to show
 *      * RedrawFrame() and now you are ready to add data
 *          * ClearGraph() to start a new data plot (erasing the last one)
 *          * Plot() methods accumulate drawings on the plot
 *      * Access the assembled image at any time with Render()
 *
 */
namespace ScottPlot
{
    public class Figure
    {
        private Bitmap BmpGraph { get; set; }

        public Axis XAxis { get; } = new Axis(-10, 10, 100);

        public Axis YAxis { get; } = new Axis(-10, 10, 100, true);

        private string BenchmarkMessage
        {
            get
            {
                double ms = stopwatch.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
                double hz = 1.0 / ms * 1000.0;
                var msg = "";
                double imageSizeMb = bmpFrame.Width * bmpFrame.Height * 4.0 / 1024 / 1024;
                msg += $"{bmpFrame.Width} x {bmpFrame.Height} ({imageSizeMb:0.00} MB) ";
                msg += $"with {pointCount:n0} data points rendered in ";
                msg += $"{ms:0.00 ms} ({hz:0.00} Hz)";

                return msg;
            }
        }
        private Color colorFigBg;
        private Color colorGraphBg;
        private Color colorAxis;
        private Color colorGridLines;

        private Point graphPos = new Point(0, 0);

        private Bitmap bmpFrame;
        private Graphics gfxFrame;
        private Graphics gfxGraph;

        private const string Font = "Arial";
        private readonly Font fontTicks = new Font(Font, 9, FontStyle.Regular);
        private readonly Font fontTitle = new Font(Font, 20, FontStyle.Bold);
        private readonly Font fontAxis = new Font(Font, 12, FontStyle.Bold);

        public string labelY = "";
        public string labelX = "";
        public string labelTitle = "";

        private int padL = 50, padT = 47, padR = 50, padB = 47;

        private readonly Stopwatch stopwatch;
        private MouseAxis mousePan;
        private MouseAxis mouseZoom;

        private long pointCount;

        // A figure object contains what's needed to draw scale bars and axis labels around a graph.
        // The graph itself is its own object which lives inside the figure.
        public Figure(int width, int height)
        {
            stopwatch = Stopwatch.StartNew();
            stopwatch.Stop();
            stopwatch.Reset();

            StyleWeb();
            Resize(width, height);

            // default to anti-aliasing on
            gfxGraph.SmoothingMode = SmoothingMode.AntiAlias;
            gfxFrame.SmoothingMode = SmoothingMode.AntiAlias;
            gfxFrame.TextRenderingHint = TextRenderingHint.AntiAlias;

            FrameRedraw();
            GraphClear();
        }

        /// <summary>
        /// Resize the entire figure (in pixels)
        /// </summary>
        public void Resize(int width, int height)
        {
            // sanity check (make sure the graph area is at least 1px by 1px
            if (width - padL - padR < 1)
            {
                width = padL + padR + 1;
            }

            if (height - padT - padB < 1)
            {
                height = padT + padB + 1;
            }

            // figure resized, so resize the frame bitmap
            bmpFrame = new Bitmap(width, height);
            gfxFrame = Graphics.FromImage(bmpFrame);

            // now re-calculate the graph size based on the padding
            FramePad(null, null, null, null);

            // now resize the graph bitmap
            BmpGraph = new Bitmap(bmpFrame.Width - padL - padR, bmpFrame.Height - padT - padB);
            gfxGraph = Graphics.FromImage(BmpGraph);

            // now resize axis to the new pad dimensions
            XAxis.Resize(BmpGraph.Width);
            YAxis.Resize(BmpGraph.Height);
        }

        /// <summary>
        /// Change the padding between the edge of the graph and edge of the figure
        /// </summary>
        private void FramePad(int? left, int? right, int? top, int? bottom)
        {
            if (left != null)
            {
                padL = (int)left;
            }

            if (right != null)
            {
                padR = (int)right;
            }

            if (top != null)
            {
                padT = (int)top;
            }

            if (bottom != null)
            {
                padB = (int)bottom;
            }

            graphPos = new Point(padL, padT);
        }

        /// <summary>
        /// Clear the frame and redraw it from scratch.
        /// </summary>
        public void FrameRedraw()
        {
            gfxFrame.Clear(colorFigBg);

            // prepare things useful for drawing
            var penAxis = new Pen(new SolidBrush(colorAxis));
            var penGrid = new Pen(colorGridLines) { DashPattern = new float[] { 4, 4 } };
            Brush brush = new SolidBrush(colorAxis);
            var sfCenter = new StringFormat
            {
                Alignment = StringAlignment.Center
            };
            var sfRight = new StringFormat
            {
                Alignment = StringAlignment.Far
            };
            int posB = BmpGraph.Height + padT;
            int posCx = BmpGraph.Width / 2 + padL;
            int posCy = BmpGraph.Height / 2 + padT;

            const int tickSizeMinor = 2;
            const int tickSizeMajor = 5;

            // draw the data rectangle and ticks
            gfxFrame.DrawRectangle(penAxis, graphPos.X - 1, graphPos.Y - 1, BmpGraph.Width + 1, BmpGraph.Height + 1);
            gfxFrame.FillRectangle(new SolidBrush(colorGraphBg), graphPos.X, graphPos.Y, BmpGraph.Width, BmpGraph.Height);
            foreach (Tick tick in XAxis.TicksMajor)
            {
                gfxFrame.DrawLine(penAxis, new Point(padL + tick.PosPixel, posB + 1), new Point(padL + tick.PosPixel, posB + 1 + tickSizeMinor));
            }

            foreach (Tick tick in YAxis.TicksMajor)
            {
                gfxFrame.DrawLine(penAxis, new Point(padL - 1, padT + tick.PosPixel), new Point(padL - 1 - tickSizeMinor, padT + tick.PosPixel));
            }

            foreach (Tick tick in XAxis.TicksMinor)
            {
                gfxFrame.DrawLine(penGrid, new Point(padL + tick.PosPixel, padT), new Point(padL + tick.PosPixel, padT + BmpGraph.Height - 1));
                gfxFrame.DrawLine(penAxis, new Point(padL + tick.PosPixel, posB + 1), new Point(padL + tick.PosPixel, posB + 1 + tickSizeMajor));
                gfxFrame.DrawString(tick.Label, fontTicks, brush, new Point(tick.PosPixel + padL, posB + 7), sfCenter);
            }

            foreach (Tick tick in YAxis.TicksMinor)
            {
                gfxFrame.DrawLine(penGrid, new Point(padL, padT + tick.PosPixel), new Point(padL + BmpGraph.Width, padT + tick.PosPixel));
                gfxFrame.DrawLine(penAxis, new Point(padL - 1, padT + tick.PosPixel), new Point(padL - 1 - tickSizeMajor, padT + tick.PosPixel));
                gfxFrame.DrawString(tick.Label, fontTicks, brush, new Point(padL - 6, tick.PosPixel + padT - 7), sfRight);
            }

            // draw labels
            gfxFrame.DrawString(labelX, fontAxis, brush, new Point(posCx, posB + 24), sfCenter);
            gfxFrame.DrawString(labelTitle, fontTitle, brush, new Point(bmpFrame.Width / 2, 8), sfCenter);
            gfxFrame.TranslateTransform(gfxFrame.VisibleClipBounds.Size.Width, 0);
            gfxFrame.RotateTransform(-90);
            gfxFrame.DrawString(labelY, fontAxis, brush, new Point(-posCy, -bmpFrame.Width + 2), sfCenter);
            gfxFrame.ResetTransform();

            // now that the frame is re-drawn, reset the graph
            GraphClear();
        }

        /// <summary>
        /// Copy the empty graph area from the frame onto the graph object
        /// </summary>
        public void GraphClear()
        {
            gfxGraph.DrawImage(bmpFrame, new Point(-padL, -padT));
            pointCount = 0;
        }

        /// <summary>
        /// Return a merged bitmap of the frame with the graph added into it
        /// </summary>
        public Bitmap Render()
        {
            var bmpMerged = new Bitmap(bmpFrame);
            Graphics gfx = Graphics.FromImage(bmpMerged);
            gfx.DrawImage(BmpGraph, graphPos);

            // draw stamp message
            if (stopwatch.ElapsedTicks > 0)
            {
                var fontStamp = new Font(Font, 8, FontStyle.Regular);
                var brushStamp = new SolidBrush(colorAxis);
                var pointStamp = new Point(bmpFrame.Width - padR - 2, bmpFrame.Height - padB - 14);
                var sfRight = new StringFormat();
                sfRight.Alignment = StringAlignment.Far;
                gfx.DrawString(BenchmarkMessage, fontStamp, brushStamp, pointStamp, sfRight);
            }

            return bmpMerged;
        }

        /// <summary>
        /// Manually define axis limits.
        /// </summary>
        public void AxisSet(double? x1, double? x2, double? y1, double? y2)
        {
            if (x1 != null)
            {
                XAxis.Min = (double)x1;
            }

            if (x2 != null)
            {
                XAxis.Max = (double)x2;
            }

            if (y1 != null)
            {
                YAxis.Min = (double)y1;
            }

            if (y2 != null)
            {
                YAxis.Max = (double)y2;
            }

            if (x1 != null || x2 != null)
            {
                XAxis.RecalculateScale();
            }

            if (y1 != null || y2 != null)
            {
                YAxis.RecalculateScale();
            }

            if (x1 != null || x2 != null || y1 != null || y2 != null)
            {
                FrameRedraw();
            }
        }

        /// <summary>
        /// Zoom in on the center of Axis by a fraction.
        /// A fraction of 2 means that the new width will be 1/2 as wide as the old width.
        /// A fraction of 0.1 means the new width will show 10 times more axis length.
        /// </summary>
        public void Zoom(double? xFrac, double? yFrac)
        {
            if (xFrac != null)
            {
                XAxis.Zoom((double)xFrac);
            }

            if (yFrac != null)
            {
                YAxis.Zoom((double)yFrac);
            }

            FrameRedraw();
        }

        private void StyleWeb()
        {
            colorFigBg = Color.White;
            colorGraphBg = Color.FromArgb(255, 235, 235, 235);
            colorAxis = Color.Black;
            colorGridLines = Color.LightGray;
        }

        public void StyleForm()
        {
            colorFigBg = SystemColors.Control;
            colorGraphBg = Color.White;
            colorAxis = Color.Black;
            colorGridLines = Color.LightGray;
        }

        /// <summary>
        /// Call this before graphing to start a stopwatch.
        /// Render time will be displayed when the output graph is rendered.
        /// </summary>
        public void BenchmarkThis(bool enable = true)
        {
            if (enable)
            {
                stopwatch.Restart();
            }
            else
            {
                stopwatch.Stop();
                stopwatch.Reset();
            }
        }

        private Point[] PointsFromArrays(double[] xs, double[] ys)
        {
            int pointCount = Math.Min(xs.Length, ys.Length);
            var points = new Point[pointCount];
            for (var i = 0; i < pointCount; i++)
            {
                points[i] = new Point(XAxis.GetPixel(xs[i]), YAxis.GetPixel(ys[i]));
            }

            return points;
        }

        public void PlotLines(double[] xs, double[] ys, float lineWidth = 1, Color? lineColor = null)
        {
            if (lineColor == null)
            {
                lineColor = Color.Red;
            }

            Point[] points = PointsFromArrays(xs, ys);
            var penLine = new Pen(new SolidBrush((Color)lineColor), lineWidth);

            // adjust the pen caps and joins to make it as smooth as possible
            penLine.StartCap = LineCap.Round;
            penLine.EndCap = LineCap.Round;
            penLine.LineJoin = LineJoin.Round;

            // todo: prevent infinite zooming overflow errors
            gfxGraph.DrawLines(penLine, points);
            pointCount += points.Length;
        }

        public void PlotSignal(double[] values, double pointSpacing = 1, double offsetX = 0, double offsetY = 0, float lineWidth = 1, Color? lineColor = null)
        {
            if (lineColor == null)
            {
                lineColor = Color.Red;
            }

            if (values == null)
            {
                return;
            }

            double lastPointX = offsetX + values.Length * pointSpacing;
            var dataMinPx = (int)((offsetX - XAxis.Min) / XAxis.UnitsPerPx);
            var dataMaxPx = (int)((lastPointX - XAxis.Min) / XAxis.UnitsPerPx);
            double binUnitsPerPx = XAxis.UnitsPerPx / pointSpacing;
            double dataPointsPerPixel = XAxis.UnitsPerPx / pointSpacing;

            var points = new List<Point>();
            var ys = new List<double>(values);

            if (dataPointsPerPixel < 1)
            {
                // LOW DENSITY TRADITIONAL X/Y PLOTTING
                var iLeft = (int)((XAxis.Min - offsetX) / XAxis.UnitsPerPx * dataPointsPerPixel);
                int iRight = iLeft + (int)(dataPointsPerPixel * BmpGraph.Width);
                for (int i = Math.Max(0, iLeft - 2); i < Math.Min(iRight + 3, ys.Count - 1); i++)
                {
                    int xPx = XAxis.GetPixel(i * pointSpacing + offsetX);
                    int yPx = YAxis.GetPixel(ys[i]);
                    points.Add(new Point(xPx, yPx));
                }
            }
            else
            {
                // BINNING IS REQUIRED FOR HIGH DENSITY PLOTTING
                for (int xPixel = Math.Max(0, dataMinPx); xPixel < Math.Min(BmpGraph.Width, dataMaxPx); xPixel++)
                {
                    var iLeft = (int)(binUnitsPerPx * (xPixel - dataMinPx));
                    var iRight = (int)(iLeft + binUnitsPerPx);
                    iLeft = Math.Max(iLeft, 0);
                    iRight = Math.Min(ys.Count - 1, iRight);
                    iRight = Math.Max(iRight, 0);
                    if (iLeft == iRight)
                    {
                        continue;
                    }

                    double yPxMin = ys.GetRange(iLeft, iRight - iLeft).Min() + offsetY;
                    double yPxMax = ys.GetRange(iLeft, iRight - iLeft).Max() + offsetY;
                    points.Add(new Point(xPixel, YAxis.GetPixel(yPxMin)));
                    points.Add(new Point(xPixel, YAxis.GetPixel(yPxMax)));
                }
            }

            if (points.Count < 2)
            {
                return;
            }

            var penLine = new Pen(new SolidBrush((Color)lineColor), lineWidth);
            const float markerSize = 3;
            var markerBrush = new SolidBrush((Color)lineColor);
            SmoothingMode originalSmoothingMode = gfxGraph.SmoothingMode;
            gfxGraph.SmoothingMode = SmoothingMode.None; // no antialiasing

            // todo: prevent infinite zooming overflow errors
            try
            {
                gfxGraph.DrawLines(penLine, points.ToArray());

                if (dataPointsPerPixel < .5)
                {
                    foreach (Point pt in points)
                    {
                        gfxGraph.FillEllipse(markerBrush, pt.X - markerSize / 2, pt.Y - markerSize / 2, markerSize, markerSize);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception plotting {0}", ex);
            }

            gfxGraph.SmoothingMode = originalSmoothingMode;
            pointCount += values.Length;
        }

        public void PlotScatter(double[] xs, double[] ys, float markerSize = 3, Color? markerColor = null)
        {
            if (markerColor == null)
            {
                markerColor = Color.Red;
            }

            Point[] points = PointsFromArrays(xs, ys);
            for (var i = 0; i < points.Length; i++)
            {
                gfxGraph.FillEllipse(new SolidBrush((Color)markerColor),
                    points[i].X - markerSize / 2,
                    points[i].Y - markerSize / 2,
                    markerSize, markerSize);
            }

            pointCount += points.Length;
        }

        public void MousePanStart(int xPx, int yPx)
        {
            mousePan = new MouseAxis(XAxis, YAxis, xPx, yPx);
        }

        public void MousePanEnd()
        {
            mousePan = null;
        }

        public void MouseZoomStart(int xPx, int yPx)
        {
            mouseZoom = new MouseAxis(XAxis, YAxis, xPx, yPx);
        }

        public void MouseZoomEnd()
        {
            mouseZoom = null;
        }

        public bool MouseIsDragging()
        {
            return mousePan != null || mouseZoom != null;
        }

        public void MouseMove(int xPx, int yPx)
        {
            if (mousePan != null)
            {
                mousePan.Pan(xPx, yPx);
                AxisSet(mousePan.X1, mousePan.X2, mousePan.Y1, mousePan.Y2);
            }
            else if (mouseZoom != null)
            {
                mouseZoom.Zoom(xPx, yPx);
                AxisSet(mouseZoom.X1, mouseZoom.X2, mouseZoom.Y1, mouseZoom.Y2);
            }
        }
    }
}