using Spectrum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SpectrumDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public unsafe partial class MainWindow : Window
    {
        private float* _fftSpectrumPtr;
        private UnsafeBuffer _fftSpectrum;
        private int _fftBins = 4096;

        private DispatcherTimer renderTimer;
        private DispatcherTimer performTimer;

        private double t;

        public MainWindow()
        {
            InitializeComponent();

            _fftSpectrum = UnsafeBuffer.Create(_fftBins, sizeof(float));
            _fftSpectrumPtr = (float*)_fftSpectrum;
        }

        ~MainWindow()
        {
            performTimer.Stop();
            _fftSpectrum.Dispose();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            performTimer = new DispatcherTimer();
            performTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            performTimer.Tick += performTimer_Tick;
            performTimer.Start();
        }        

        void renderTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < _fftBins; i++)
            {
                double x = 10.0 * Math.PI * (i - _fftBins / 2.0) / _fftBins;
                _fftSpectrumPtr[i] = (float)(30.0 * Math.Cos(x) - 20.0 * (Math.Cos(Math.PI * t) + 1.0) - 40.0);
            }

            t += 0.015;

            if (t > 1.0)
                t = -1.0;

            spectrumAnalyzer.Render(_fftSpectrumPtr, _fftBins);
            waterfall.Render(_fftSpectrumPtr, _fftBins);
        }

        void performTimer_Tick(object sender, EventArgs e)
        {
            spectrumAnalyzer.Perform();
            waterfall.Perform();
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            if (renderTimer == null)
            {
                renderTimer = new DispatcherTimer();
                renderTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
                renderTimer.Tick += renderTimer_Tick;
                renderTimer.Start();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (renderTimer != null)
            {
                renderTimer.Stop();
                renderTimer = null;
            }
        }
    }
}
