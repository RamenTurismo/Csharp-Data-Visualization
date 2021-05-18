using Accord.Math;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

namespace ScottPlotMicrophoneFFT
{
    public partial class Form1 : Form
    {
        private const int Rate = 44100; // sample rate of the sound card
        private static readonly int Buffersize = (int)Math.Pow(2, 11); // must be a multiple of 2
        private static readonly int BufferMiliseconds = (int)(Buffersize / (double)Rate * 1000d);
        private BufferedWaveProvider bwp;
        private int numberOfDraws;
        private bool needsAutoScaling = true;

        public Form1()
        {
            InitializeComponent();
            SetupGraphLabels();
            StartListeningToMicrophone();
            timerReplot.Enabled = true;
        }

        private void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        private void SetupGraphLabels()
        {
            scottPlotUC1.Fig.labelTitle = "Microphone PCM Data";
            scottPlotUC1.Fig.labelY = "Amplitude (PCM)";
            scottPlotUC1.Fig.labelX = "Time (ms)";
            scottPlotUC1.Redraw();

            scottPlotUC2.Fig.labelTitle = "Microphone FFT Data";
            scottPlotUC2.Fig.labelY = "Power (raw)";
            scottPlotUC2.Fig.labelX = "Frequency (Hz)";
            scottPlotUC2.Redraw();
        }

        private void StartListeningToMicrophone(int audioDeviceNumber = 0)
        {
            var wi = new WaveIn
            {
                DeviceNumber = audioDeviceNumber,
                WaveFormat = new WaveFormat(Rate, 1),
                BufferMilliseconds = BufferMiliseconds
            };

            wi.DataAvailable += AudioDataAvailable;

            bwp = new BufferedWaveProvider(wi.WaveFormat)
            {
                BufferLength = Buffersize * 2,
                DiscardOnBufferOverflow = true
            };

            try
            {
                wi.StartRecording();
            }
            catch
            {
                var msg = "Could not record from audio device!\n\n";
                msg += "Is your microphone plugged in?\n";
                msg += "Is it set as your default recording device?";
                MessageBox.Show(msg, "ERROR");
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // turn off the timer, take as long as we need to plot, then turn the timer back on
            timerReplot.Enabled = false;
            PlotLatestData();
            timerReplot.Enabled = true;
        }

        private void PlotLatestData()
        {
            // check the incoming microphone audio
            int frameSize = Buffersize;
            var audioBytes = new byte[frameSize];
            bwp.Read(audioBytes, 0, frameSize);

            // return if there's nothing new to plot
            if (audioBytes.Length == 0)
            {
                return;
            }

            if (audioBytes[frameSize - 2] == 0)
            {
                return;
            }

            // incoming data is 16-bit (2 bytes per audio point)
            const int bytesPerPoint = 2;

            // create a (32-bit) int array ready to fill with the 16-bit data
            int graphPointCount = audioBytes.Length / bytesPerPoint;

            // create double arrays to hold the data we will graph
            var pcm = new double[graphPointCount];
            var fftReal = new double[graphPointCount / 2];

            // populate Xs and Ys with double data
            for (var i = 0; i < graphPointCount; i++)
            {
                // read the int16 from the two bytes
                var val = BitConverter.ToInt16(audioBytes, i * 2);

                // store the value in Ys as a percent (+/- 100% = 200%)
                pcm[i] = val / Math.Pow(2, 16) * 200.0;
            }

            // calculate the full FFT
            double[] fft = Fft(pcm);

            // determine horizontal axis units for graphs
            const double pcmPointSpacingMs = Rate / 1000d;
            const double fftMaxFreq = Rate / 2d;
            double fftPointSpacingHz = fftMaxFreq / graphPointCount;

            // just keep the real half (the other half imaginary)
            Array.Copy(fft, fftReal, fftReal.Length);

            // plot the Xs and Ys for both graphs
            scottPlotUC1.Clear();
            scottPlotUC1.PlotSignal(pcm, pcmPointSpacingMs, Color.Blue);
            scottPlotUC2.Clear();
            scottPlotUC2.PlotSignal(fftReal, fftPointSpacingHz, Color.Blue);

            // optionally adjust the scale to automatically fit the data
            if (needsAutoScaling)
            {
                scottPlotUC1.AxisAuto();
                scottPlotUC2.AxisAuto();
                needsAutoScaling = false;
            }

            //scottPlotUC1.PlotSignal(Ys, RATE);

            numberOfDraws += 1;
            lblStatus.Text = $"Analyzed and graphed PCM and FFT data {numberOfDraws} times";

            // this reduces flicker and helps keep the program responsive
            Application.DoEvents();
        }

        private void AutoScaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            needsAutoScaling = true;
        }

        private void InfoMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var msg = "";
            msg += "left-click-drag to pan\n";
            msg += "right-click-drag to zoom\n";
            msg += "middle-click to auto-axis\n";
            msg += "double-click for graphing stats\n";
            MessageBox.Show(msg);
        }

        private void WebsiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/swharden/Csharp-Data-Visualization");
        }

        private static double[] Fft(IReadOnlyList<double> data)
        {
            var fft = new double[data.Count];
            var fftComplex = new Complex[data.Count];
            for (var i = 0; i < data.Count; i++)
            {
                fftComplex[i] = new Complex(data[i], 0.0);
            }

            FourierTransform.FFT(fftComplex, FourierTransform.Direction.Forward);
            for (var i = 0; i < data.Count; i++)
            {
                fft[i] = fftComplex[i].Magnitude;
            }

            return fft;
        }
    }
}