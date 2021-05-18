using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ScottPlot
{
    public partial class ScottPlotUC : UserControl
    {
        public Figure Fig { get; } = new Figure(123, 123);

        private readonly List<SignalData> signalDataList = new List<SignalData>();

        //private List<XYData> xyDataList = new List<XYData>();
        private readonly List<AxisLine> hLines = new List<AxisLine>();
        private readonly List<AxisLine> vLines = new List<AxisLine>();
        private readonly List<XYData> xyDataList = new List<XYData>();
        private bool showBenchmark;
        private bool busyDrawingPlot;

        public void Hline(double Ypos, float lineWidth, Color lineColor)
        {
            hLines.Add(new AxisLine(Ypos, lineWidth, lineColor));
            Render();
        }

        public void Vline(double Xpos, float lineWidth, Color lineColor)
        {
            vLines.Add(new AxisLine(Xpos, lineWidth, lineColor));
            Render();
        }

        public void PlotXY(double[] Xs, double[] Ys, Color? color = null)
        {
            //xyDataList.Add(new XYData(Xs, Ys, lineColor: color, markerColor: color));
            Fig.GraphClear();
            Render();
        }

        public void PlotSignal(double[] values, double sampleRate, Color? color = null, double offsetX = 0, double offsetY = 0)
        {
            signalDataList.Add(new SignalData(values, sampleRate, lineColor: color, offsetX: offsetX, offsetY: offsetY));
            Fig.GraphClear();
            Render();
        }

        public void Clear(bool renderAfterClearing = false)
        {
            //xyDataList.Clear();
            signalDataList.Clear();
            hLines.Clear();
            vLines.Clear();
            if (renderAfterClearing)
            {
                Render();
            }
        }

        public void SaveDialog(string filename = "output.png")
        {
            var savefile = new SaveFileDialog();
            savefile.FileName = filename;
            savefile.Filter = "PNG Files (*.png)|*.png|All files (*.*)|*.*";
            if (savefile.ShowDialog() == DialogResult.OK)
            {
                filename = savefile.FileName;
            }
            else
            {
                return;
            }

            string basename = Path.GetFileNameWithoutExtension(filename);
            string extension = Path.GetExtension(filename).ToLower();
            string fullPath = Path.GetFullPath(filename);

            switch (extension)
            {
                case ".png":
                    pictureBox1.Image.Save(filename, ImageFormat.Png);

                    break;
                case ".jpg":
                    pictureBox1.Image.Save(filename, ImageFormat.Jpeg);

                    break;
                case ".bmp":
                    pictureBox1.Image.Save(filename);

                    break;
            }
        }

        public ScottPlotUC()
        {
            InitializeComponent();

            // add a mousewheel scroll handler
            pictureBox1.MouseWheel += PictureBox1_MouseWheel;

            // style the plot area
            Fig.StyleForm();
            Fig.Zoom(.8, .8);
            Fig.labelTitle = "ScottPlot User Control";
        }

        public void AxisAuto()
        {
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;

            foreach (XYData xyData in xyDataList)
            {
                if (x1 == x2)
                {
                    // this is the first data we are scaling to, so just copy its bounds
                    x1 = xyData.Xs.Min();
                    x2 = xyData.Xs.Max();
                    y1 = xyData.Ys.Min();
                    y2 = xyData.Ys.Max();
                }
                else
                {
                    // we've seen some data before, so only take it if it expands the axes
                    x1 = Math.Min(x1, xyData.Xs.Min());
                    x2 = Math.Max(x2, xyData.Xs.Max());
                    y1 = Math.Min(y1, xyData.Ys.Min());
                    y2 = Math.Max(y2, xyData.Ys.Max());
                }
            }
            foreach (SignalData signalData in signalDataList)
            {
                if (x1 == x2)
                {
                    // this is the first data we are scaling to, so just copy its bounds
                    x1 = signalData.OffsetX;
                    x2 = signalData.OffsetX + signalData.Values.Length * signalData.XSpacing;
                    y1 = signalData.Values.Min() + signalData.OffsetY;
                    y2 = signalData.Values.Max() + signalData.OffsetY;
                }
                else
                {
                    // we've seen some data before, so only take it if it expands the axes
                    x1 = Math.Min(x1, signalData.OffsetX);
                    x2 = Math.Max(x2, signalData.OffsetX + signalData.Values.Length * signalData.XSpacing);
                    y1 = Math.Min(y1, signalData.Values.Min() + signalData.OffsetY);
                    y2 = Math.Max(y2, signalData.Values.Max() + signalData.OffsetY);
                }
            }

            Fig.AxisSet(x1, x2, y1, y2);
            Fig.Zoom(null, .9);
            Render(true);
        }
        private class XYData
        {
            public double[] Xs;
            public double[] Ys;
            public float lineWidth;
            public Color lineColor;
            public float markerSize;
            public Color markerColor;
            public string label;

            public XYData(double[] Xs, double[] Ys, float lineWidth = 1, Color? lineColor = null, float markerSize = 3, Color? markerColor = null, string label = null)
            {
                this.Xs = Xs;
                this.Ys = Ys;
                this.lineWidth = lineWidth;
                this.markerSize = markerSize;
                this.label = label;
                if (lineColor == null) lineColor = Color.Red;
                this.lineColor = (Color)lineColor;
                if (markerColor == null) markerColor = Color.Red;
                this.markerColor = (Color)markerColor;
            }
        }

        private void Render(bool redrawFrame = false)
        {
            Fig.BenchmarkThis(showBenchmark);
            if (redrawFrame)
            {
                Fig.FrameRedraw();
            }
            else
            {
                Fig.GraphClear();
            }

            // plot XY points
            foreach (XYData xyData in xyDataList)
            {
                Fig.PlotLines(xyData.Xs, xyData.Ys, xyData.lineWidth, xyData.lineColor);
                Fig.PlotScatter(xyData.Xs, xyData.Ys, xyData.markerSize, xyData.markerColor);
            }

            // plot signals
            foreach (SignalData signalData in signalDataList)
            {
                Fig.PlotSignal(signalData.Values, signalData.XSpacing, signalData.OffsetX, signalData.OffsetY, signalData.LineWidth, signalData.LineColor);
            }

            // plot axis lines
            foreach (AxisLine axisLine in hLines)
            {
                Fig.PlotLines(
                    new[] { Fig.XAxis.Min, Fig.XAxis.Max },
                    new[] { axisLine.Value, axisLine.Value },
                    axisLine.LineWidth,
                    axisLine.LineColor
                );
            }

            foreach (AxisLine axisLine in vLines)
            {
                Fig.PlotLines(
                    new[] { axisLine.Value, axisLine.Value },
                    new[] { Fig.YAxis.Min, Fig.YAxis.Max },
                    axisLine.LineWidth,
                    axisLine.LineColor
                );
            }

            pictureBox1.Image = Fig.Render();
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    Fig.MousePanStart(e.X, e.Y); // left-click-drag pans

                    break;
                case MouseButtons.Right:
                    Fig.MouseZoomStart(e.X, e.Y); // right-click-drag zooms

                    break;
            }
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    Fig.MousePanEnd();

                    break;
                case MouseButtons.Right:
                    Fig.MouseZoomEnd();

                    break;
            }
        }

        private void PictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                AxisAuto(); // middle click to reset view
            }
        }

        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            const double mag = 1.2;
            if (e.Delta > 0)
            {
                Fig.Zoom(mag, mag);
            }
            else
            {
                Fig.Zoom(1.0 / mag, 1.0 / mag);
            }

            Render();
        }

        private void PictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            showBenchmark = !showBenchmark; // double-click graph to display benchmark stats
            Render();
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (Fig.MouseIsDragging() && busyDrawingPlot == false)
            {
                Fig.MouseMove(e.X, e.Y);
                busyDrawingPlot = true;
                Render(true);
                Application.DoEvents();
                busyDrawingPlot = false;
            }
        }

        private void PictureBox1_SizeChanged(object sender, EventArgs e)
        {
            Fig.Resize(pictureBox1.Width, pictureBox1.Height);
            Render(true);
        }

        /// <summary>
        /// Force ScottPlot to redraw itself. This is helpful after changing axis limits or labels.
        /// </summary>
        public void Redraw()
        {
            PictureBox1_SizeChanged(null, null);
        }
    }
}