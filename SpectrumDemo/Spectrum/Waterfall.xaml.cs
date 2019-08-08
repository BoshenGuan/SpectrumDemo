using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace Spectrum
{
    /// <summary>
    /// Waterfall.xaml 的交互逻辑
    /// </summary>
    public partial class Waterfall : UserControl
    {
        private const float TrackingFontSize = 16.0f;
        private const float TimestampFontSize = 14.0f;
        private const int CarrierPenWidth = 1;
        private const int AxisMargin = 30;

        private double _attack;
        private double _decay;
        private bool _performNeeded;
        private Bitmap _buffer;
        private Bitmap _buffer2;
        private Graphics _graphics;
        private Graphics _graphics2;
        private BandType _bandType;
        private int _filterBandwidth;
        private int _filterOffset;
        private float _xIncrement;
        private byte[] _temp;
        private byte[] _powerSpectrum;
        private byte[] _scaledPowerSpectrum;
        private long _centerFrequency;
        private long _spectrumWidth;
        private int _stepSize;
        private long _frequency;
        private float _lower;
        private float _upper;
        private float _scale = 1f;
        private long _displayCenterFrequency;
        private bool _changingBandwidth;
        private bool _changingFrequency;
        private bool _changingCenterFrequency;
        private bool _mouseIn;
        private int _oldX;
        private long _oldFrequency;
        private long _oldCenterFrequency;
        private int _oldFilterBandwidth;
        private int[] _gradientPixels;
        private int _contrast;
        private int _zoom;
        private bool _useSmoothing;
        private bool _useSnap;
        private int _trackingY;
        private int _trackingX;
        private long _trackingFrequency;
        private bool _useTimestamps;
        private int _scanlines;
        private int _timestampInterval;
        private int _displayRange = 130;
        private int _displayOffset;
        private LinearGradientBrush _gradientBrush;
        private ColorBlend _gradientColorBlend = GetGradientBlend();
        private WriteableBitmap _writeableBitmap;
        private bool _initialized;

        public Waterfall()
        {
            InitializeComponent();
        }

        public static ColorBlend GetGradientBlend()
        {
            return Utils.GetGradientBlend(255);
        }

        ~Waterfall()
        {
            _buffer.Dispose();
            _buffer2.Dispose();
            _graphics.Dispose();
            _graphics2.Dispose();
            _gradientBrush.Dispose();
        }

        public void Perform()
        {
            if (_performNeeded && _mouseIn)
            {
                CopyMainBuffer();
                DrawCursor();
                InvalidateVisual();
                _performNeeded = false;
            }
        }

        public event ManualFrequencyChange FrequencyChanged;

        public event ManualFrequencyChange CenterFrequencyChanged;

        public event ManualBandwidthChange BandwidthChanged;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ColorBlend GradientColorBlend
        {
            get
            {
                return _gradientColorBlend;
            }
            set
            {
                if (_gradientColorBlend != value)
                {
                    Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

                    _gradientColorBlend = value;

                    _gradientBrush.Dispose();
                    _gradientBrush = new LinearGradientBrush(new Rectangle(AxisMargin / 2, AxisMargin / 2, ClientRectangle.Width - AxisMargin / 2, ClientRectangle.Height - AxisMargin / 2), Color.White, Color.Black, LinearGradientMode.Vertical);
                    _gradientPixels = null;
                    _gradientBrush.InterpolationColors = _gradientColorBlend;

                    DrawGradient();
                    BuildGradientVector();

                    _performNeeded = true;
                }
            }
        }

        public long CenterFrequency
        {
            get
            {
                return _centerFrequency;
            }
            set
            {
                if (_centerFrequency != value)
                {
                    _displayCenterFrequency += value - _centerFrequency;
                    _centerFrequency = value;
                    _performNeeded = true;
                }
            }
        }

        public int SpectrumWidth
        {
            get
            {
                return (int)_spectrumWidth;
            }
            set
            {
                if (_spectrumWidth != value)
                {
                    _spectrumWidth = value;
                    ApplyZoom();
                }
            }
        }

        public long Frequency
        {
            get
            {
                return _frequency;
            }
            set
            {
                if (_frequency != value)
                {
                    _frequency = value;
                    _performNeeded = true;
                }
            }
        }

        public int FilterBandwidth
        {
            get
            {
                return _filterBandwidth;
            }
            set
            {
                if (_filterBandwidth != value)
                {
                    _filterBandwidth = value;
                    _performNeeded = true;
                }
            }
        }

        public int DisplayRange
        {
            get { return _displayRange; }
            set { _displayRange = value; }
        }

        public int DisplayOffset
        {
            get { return _displayOffset; }
            set { _displayOffset = value; }
        }

        public int FilterOffset
        {
            get
            {
                return _filterOffset;
            }
            set
            {
                if (_filterOffset != value)
                {
                    _filterOffset = value;
                    _performNeeded = true;
                }
            }
        }

        public BandType BandType
        {
            get
            {
                return _bandType;
            }
            set
            {
                if (_bandType != value)
                {
                    _bandType = value;
                    _performNeeded = true;
                }
            }
        }

        public int Contrast
        {
            get
            {
                return _contrast;
            }
            set
            {
                _contrast = value;
            }
        }

        public int Zoom
        {
            get
            {
                return _zoom;
            }
            set
            {
                if (_zoom != value)
                {
                    _zoom = value;
                    ApplyZoom();
                }
            }
        }

        public bool UseSmoothing
        {
            get { return _useSmoothing; }
            set { _useSmoothing = value; }
        }

        public double Decay
        {
            get { return _decay; }
            set { _decay = value; }
        }

        public double Attack
        {
            get { return _attack; }
            set { _attack = value; }
        }

        public int StepSize
        {
            get { return _stepSize; }
            set
            {
                _performNeeded = true;
                _stepSize = value;
            }
        }

        public bool UseSnap
        {
            get { return _useSnap; }
            set { _useSnap = value; }
        }

        public bool UseTimestamps
        {
            get { return _useTimestamps; }
            set
            {
                _useTimestamps = value;
                _scanlines = 0;
            }
        }

        public int TimestampInterval
        {
            get { return _timestampInterval; }
            set { _timestampInterval = value; }
        }

        private void ApplyZoom()
        {
            _scale = (float)Math.Pow(10, _zoom * Utils.MaxZoom / 100.0f);
            _displayCenterFrequency = GetDisplayCenterFrequency();
            if (_spectrumWidth > 0)
            {
                Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

                _xIncrement = _scale * (ClientRectangle.Width - 2 * AxisMargin) / _spectrumWidth;
                _performNeeded = true;
            }
        }

        public void CenterZoom()
        {
            _displayCenterFrequency = GetDisplayCenterFrequency();
        }

        private long GetDisplayCenterFrequency()
        {
            var f = _frequency;
            switch (_bandType)
            {
                case BandType.Lower:
                    f -= _filterBandwidth / 2 + _filterOffset;
                    break;

                case BandType.Upper:
                    f += _filterBandwidth / 2 + _filterOffset;
                    break;
            }
            var lowerLeadingSpectrum = (long)((_centerFrequency - _spectrumWidth / 2) - (f - _spectrumWidth / _scale / 2));
            if (lowerLeadingSpectrum > 0)
            {
                f += lowerLeadingSpectrum + 10;
            }

            var upperLeadingSpectrum = (long)((f + _spectrumWidth / _scale / 2) - (_centerFrequency + _spectrumWidth / 2));
            if (upperLeadingSpectrum > 0)
            {
                f -= upperLeadingSpectrum + 10;
            }

            return f;
        }

        public unsafe void Render(float* powerSpectrum, int length)
        {
            if (_scaledPowerSpectrum == null || _scaledPowerSpectrum.Length != length)
            {
                _scaledPowerSpectrum = new byte[length];
            }
            fixed (byte* scaledPowerSpectrumPtr = _scaledPowerSpectrum)
            {
                var displayOffset = _displayOffset / 10 * 10;
                var displayRange = _displayRange / 10 * 10;
                Fourier.ScaleFFT(powerSpectrum, scaledPowerSpectrumPtr, length, displayOffset - displayRange, displayOffset);
            }
            var scaledLength = (int)(length / _scale);
            var offset = (int)((length - scaledLength) / 2.0 + length * (double)(_displayCenterFrequency - _centerFrequency) / _spectrumWidth);
            if (_useSmoothing)
            {
                Fourier.SmoothCopy(_scaledPowerSpectrum, _temp, length, _scale, offset);
                for (var i = 0; i < _powerSpectrum.Length; i++)
                {
                    var ratio = _powerSpectrum[i] < _temp[i] ? Attack : Decay;
                    _powerSpectrum[i] = (byte)Math.Round(_powerSpectrum[i] * (1 - ratio) + _temp[i] * ratio);
                }
            }
            else
            {
                Fourier.SmoothCopy(_scaledPowerSpectrum, _powerSpectrum, length, _scale, offset);
            }

            Draw();
            InvalidateVisual();
        }

        private void Draw()
        {
            #region Draw only if needed

            Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

            if (ClientRectangle.Width <= AxisMargin || ClientRectangle.Height <= AxisMargin)
            {
                return;
            }

            #endregion


            #region Shift image

            ShiftImage();

            #endregion

            #region Draw Spectrum

            DrawSpectrum();

            #endregion

            #region Timestamps

            if (_useTimestamps && ++_scanlines >= TimestampInterval)
            {
                _scanlines = 0;
                DrawTimestamp();
            }

            #endregion

            #region Draw gradient

            DrawGradient();

            #endregion

            #region Draw cursor

            if (_mouseIn)
            {
                CopyMainBuffer();
                DrawCursor();
            }

            #endregion
        }

        private unsafe void ShiftImage()
        {
            Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

            var bmpData = _buffer.LockBits(ClientRectangle, ImageLockMode.ReadWrite, _buffer.PixelFormat);
            void* src;
            void* dest;
            if (bmpData.Stride > 0)
            {
                src = (void*)bmpData.Scan0;
                dest = (void*)((long)bmpData.Scan0 + bmpData.Stride);
            }
            else
            {
                dest = (void*)bmpData.Scan0;
                src = (void*)((long)bmpData.Scan0 - bmpData.Stride);
            }
            Utils.Memmove(dest, src, (bmpData.Height - 1) * Math.Abs(bmpData.Stride));
            _buffer.UnlockBits(bmpData);
        }

        private unsafe void DrawSpectrum()
        {
            if (_powerSpectrum == null || _powerSpectrum.Length == 0)
            {
                return;
            }

            Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

            var bits = _buffer.LockBits(ClientRectangle, ImageLockMode.ReadWrite, _buffer.PixelFormat);
            int* ptr;
            if (bits.Stride > 0)
            {
                ptr = (int*)bits.Scan0 + AxisMargin;
            }
            else
            {
                ptr = (int*)((long)bits.Scan0 - bits.Stride * (bits.Height - 1)) + AxisMargin;
            }
            for (var i = 0; i < _powerSpectrum.Length; i++)
            {
                var colorIndex = (int)((_powerSpectrum[i] + _contrast * 50.0 / 25.0) * _gradientPixels.Length / byte.MaxValue);
                colorIndex = Math.Max(colorIndex, 0);
                colorIndex = Math.Min(colorIndex, _gradientPixels.Length - 1);

                *ptr++ = _gradientPixels[colorIndex];
            }
            _buffer.UnlockBits(bits);
        }

        private void DrawTimestamp()
        {
            using (var fontFamily = new FontFamily("Arial"))
            using (var path = new GraphicsPath())
            using (var outlinePen = new Pen(Color.Black))
            {
                var timestamp = DateTime.Now.ToString();

                path.AddString(timestamp, fontFamily, (int)System.Drawing.FontStyle.Regular, TimestampFontSize, new System.Drawing.Point(AxisMargin, 0), StringFormat.GenericTypographic);
                var smoothingMode = _graphics.SmoothingMode;
                var interpolationMode = _graphics.InterpolationMode;
                _graphics.SmoothingMode = SmoothingMode.AntiAlias;
                _graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                outlinePen.Width = 2;
                _graphics.DrawPath(outlinePen, path);
                _graphics.FillPath(Brushes.White, path);
                _graphics.SmoothingMode = smoothingMode;
                _graphics.InterpolationMode = interpolationMode;
            }
        }

        public static string GetFrequencyDisplay(long frequency)
        {
            string result;
            if (frequency == 0)
            {
                result = "DC";
            }
            else if (Math.Abs(frequency) > 1500000000)
            {
                result = string.Format("{0:#,0.000 000}GHz", frequency / 1000000000.0);
            }
            else if (Math.Abs(frequency) > 30000000)
            {
                result = string.Format("{0:0,0.000#}MHz", frequency / 1000000.0);
            }
            else if (Math.Abs(frequency) > 1000)
            {
                result = string.Format("{0:#,#.###}kHz", frequency / 1000.0);
            }
            else
            {
                result = string.Format("{0}Hz", frequency);
            }
            return result;
        }

        public static void ConfigureGraphics(Graphics graphics)
        {
            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            graphics.InterpolationMode = InterpolationMode.High;
        }

        private void DrawCursor()
        {
            Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

            _lower = 0f;
            float bandpassOffset;
            var bandpassWidth = 0f;
            var cursorWidth = Math.Max((_filterBandwidth + _filterOffset) * _xIncrement, 2);
            var xCarrier = (float)ClientRectangle.Width / 2 + (_frequency - _displayCenterFrequency) * _xIncrement;

            switch (_bandType)
            {
                case BandType.Upper:
                    bandpassOffset = _filterOffset * _xIncrement;
                    bandpassWidth = cursorWidth - bandpassOffset;
                    _lower = xCarrier + bandpassOffset;
                    break;

                case BandType.Lower:
                    bandpassOffset = _filterOffset * _xIncrement;
                    bandpassWidth = cursorWidth - bandpassOffset;
                    _lower = xCarrier - bandpassOffset - bandpassWidth;
                    break;

                case BandType.Center:
                    _lower = xCarrier - cursorWidth / 2;
                    bandpassWidth = cursorWidth;
                    break;
            }
            _upper = _lower + bandpassWidth;

            using (var transparentBrush = new SolidBrush(Color.FromArgb(80, Color.DarkGray)))
            using (var hotTrackPen = new Pen(Color.Red))
            using (var carrierPen = new Pen(Color.Red))
            using (var fontFamily = new FontFamily("Arial"))
            using (var path = new GraphicsPath())
            using (var outlinePen = new Pen(Color.Black))
            {
                carrierPen.Width = CarrierPenWidth;
                if (cursorWidth < ClientRectangle.Width)
                {
                    _graphics2.FillRectangle(transparentBrush, (int)_lower + 1, 0, (int)bandpassWidth, ClientRectangle.Height);
                    if (xCarrier >= AxisMargin && xCarrier <= ClientRectangle.Width - AxisMargin)
                    {
                        _graphics2.DrawLine(carrierPen, xCarrier, 0, xCarrier, ClientRectangle.Height);
                    }
                }
                if (_trackingX >= AxisMargin && _trackingX <= ClientRectangle.Width - AxisMargin)
                {
                    if (!_changingFrequency && !_changingCenterFrequency && !_changingBandwidth)
                    {
                        _graphics2.DrawLine(hotTrackPen, _trackingX, 0, _trackingX, ClientRectangle.Height);
                    }

                    string fstring;
                    if (_changingFrequency)
                    {
                        fstring = "VFO = " + GetFrequencyDisplay(_frequency);
                    }
                    else if (_changingBandwidth)
                    {
                        fstring = "BW = " + GetFrequencyDisplay(_filterBandwidth);
                    }
                    else if (_changingCenterFrequency)
                    {
                        fstring = "Center Freq. = " + GetFrequencyDisplay(_centerFrequency);
                    }
                    else
                    {
                        fstring = GetFrequencyDisplay(_trackingFrequency);
                    }

                    path.AddString(fstring, fontFamily, (int)System.Drawing.FontStyle.Regular, TrackingFontSize, System.Drawing.Point.Empty, StringFormat.GenericTypographic);
                    var stringSize = path.GetBounds();
                    var cursorHeight = SystemParameters.CursorHeight;
                    var xOffset = _trackingX + 15.0f;
                    var yOffset = _trackingY + cursorHeight - 8.0f;
                    xOffset = Math.Min(xOffset, ClientRectangle.Width - stringSize.Width - 5);
                    yOffset = Math.Min(yOffset, ClientRectangle.Height - stringSize.Height - 5);
                    path.Reset();
                    path.AddString(fstring, fontFamily, (int)System.Drawing.FontStyle.Regular, TrackingFontSize, new System.Drawing.Point((int)xOffset, (int)yOffset), StringFormat.GenericTypographic);
                    var smoothingMode = _graphics2.SmoothingMode;
                    var interpolationMode = _graphics2.InterpolationMode;
                    _graphics2.SmoothingMode = SmoothingMode.AntiAlias;
                    _graphics2.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    outlinePen.Width = 2;
                    _graphics2.DrawPath(outlinePen, path);
                    _graphics2.FillPath(Brushes.White, path);
                    _graphics2.SmoothingMode = smoothingMode;
                    _graphics2.InterpolationMode = interpolationMode;
                }
            }
        }

        private unsafe void CopyMainBuffer()
        {
            var rect = new Rectangle(0, 0, _buffer.Width, _buffer.Height);
            var data1 = _buffer.LockBits(rect, ImageLockMode.ReadOnly, _buffer.PixelFormat);
            var data2 = _buffer2.LockBits(rect, ImageLockMode.WriteOnly, _buffer2.PixelFormat);
            Utils.Memcpy((void*)data2.Scan0, (void*)data1.Scan0, Math.Abs(data1.Stride) * data1.Height);
            _buffer.UnlockBits(data1);
            _buffer2.UnlockBits(data2);
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(RenderSize));

            if (_initialized)
            {
                Bitmap bitmap = _mouseIn ? _buffer2 : _buffer;

                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
                _writeableBitmap.Lock();
                Utils.CopyMemory(_writeableBitmap.BackBuffer, data.Scan0, (UInt32)(_writeableBitmap.BackBufferStride * bitmap.Height));
                _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.Width, bitmap.Height));
                _writeableBitmap.Unlock();
                bitmap.UnlockBits(data);

                drawingContext.DrawImage(_writeableBitmap, new System.Windows.Rect(0, 0, bitmap.Width, bitmap.Height));
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

            if (ClientRectangle.Width <= AxisMargin || ClientRectangle.Height <= AxisMargin)
            {
                return;
            }

            if (!_initialized)
            {
                _powerSpectrum = new byte[ClientRectangle.Width - 2 * AxisMargin];
                _temp = new byte[_powerSpectrum.Length];
                _buffer = new Bitmap(ClientRectangle.Width, ClientRectangle.Height, PixelFormat.Format32bppPArgb);
                _buffer2 = new Bitmap(ClientRectangle.Width, ClientRectangle.Height, PixelFormat.Format32bppPArgb);
                _graphics = Graphics.FromImage(_buffer);
                _graphics2 = Graphics.FromImage(_buffer2);
                _gradientBrush = new LinearGradientBrush(new Rectangle(AxisMargin / 2, AxisMargin / 2, ClientRectangle.Width - AxisMargin / 2, ClientRectangle.Height - AxisMargin / 2), Color.White, Color.Black, LinearGradientMode.Vertical);
                _gradientBrush.InterpolationColors = _gradientColorBlend;
                _writeableBitmap = new WriteableBitmap(ClientRectangle.Width, ClientRectangle.Height, _buffer.HorizontalResolution, _buffer.VerticalResolution, System.Windows.Media.PixelFormats.Bgr32, null);
                _initialized = true;
            }
            else
            {
                var temp = new byte[ClientRectangle.Width - 2 * AxisMargin];
                Fourier.SmoothCopy(_powerSpectrum, temp, _powerSpectrum.Length, (_temp.Length + temp.Length) / (float)_temp.Length, 0);
                _powerSpectrum = temp;
                _temp = new byte[_powerSpectrum.Length];

                var oldBuffer = _buffer;
                _buffer = new Bitmap(ClientRectangle.Width, ClientRectangle.Height, PixelFormat.Format32bppPArgb);
                var oldBuffer2 = _buffer2;
                _buffer2 = new Bitmap(ClientRectangle.Width, ClientRectangle.Height, PixelFormat.Format32bppPArgb);

                _writeableBitmap = new WriteableBitmap(ClientRectangle.Width, ClientRectangle.Height, _buffer.HorizontalResolution, _buffer.VerticalResolution, System.Windows.Media.PixelFormats.Bgr32, null);

                _graphics.Dispose();
                _graphics = Graphics.FromImage(_buffer);
                ConfigureGraphics(_graphics);

                _graphics2.Dispose();
                _graphics2 = Graphics.FromImage(_buffer2);
                ConfigureGraphics(_graphics2);

                _graphics.Clear(Color.Black);
                var rect = new Rectangle(AxisMargin, 0, _buffer.Width - 2 * AxisMargin, _buffer.Height);
                _graphics.DrawImage(oldBuffer, rect, AxisMargin, 0, oldBuffer.Width - 2 * AxisMargin, oldBuffer.Height, GraphicsUnit.Pixel);
                oldBuffer.Dispose();
                oldBuffer2.Dispose();
                if (_spectrumWidth > 0)
                {
                    _xIncrement = _scale * (ClientRectangle.Width - 2 * AxisMargin) / _spectrumWidth;
                }
                _gradientBrush.Dispose();
                _gradientBrush = new LinearGradientBrush(new Rectangle(AxisMargin / 2, AxisMargin / 2, ClientRectangle.Width - AxisMargin / 2, ClientRectangle.Height - AxisMargin / 2), Color.White, Color.Black, LinearGradientMode.Vertical);
                _gradientPixels = null;
                _gradientBrush.InterpolationColors = _gradientColorBlend;
            }

            GC.Collect();

            DrawGradient();
            BuildGradientVector();
            _performNeeded = true;
            var oldMouseIn = _mouseIn;
            _mouseIn = true;
            Perform();
            _mouseIn = oldMouseIn;
        }

        private void DrawGradient()
        {
            using (var pen = new Pen(_gradientBrush, 10))
            {
                Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

                _graphics.FillRectangle(Brushes.Black,
                                   ClientRectangle.Width - AxisMargin,
                                   0,
                                   AxisMargin,
                                   ClientRectangle.Height);
                _graphics.DrawLine(pen,
                                   ClientRectangle.Width - AxisMargin / 2,
                                   ClientRectangle.Height - AxisMargin / 2,
                                   ClientRectangle.Width - AxisMargin / 2,
                                   AxisMargin / 2);
            }
        }

        private void BuildGradientVector()
        {
            Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

            if (_gradientPixels == null || _gradientPixels.Length != ClientRectangle.Height - AxisMargin)
            {
                _gradientPixels = new int[ClientRectangle.Height - AxisMargin - 1];
            }
            for (var i = 0; i < _gradientPixels.Length; i++)
            {
                _gradientPixels[_gradientPixels.Length - i - 1] = _buffer.GetPixel(ClientRectangle.Width - AxisMargin / 2, i + AxisMargin / 2 + 1).ToArgb();
            }
        }

        protected virtual void OnFrequencyChanged(FrequencyEventArgs e)
        {
            if (FrequencyChanged != null)
            {
                FrequencyChanged(this, e);
            }
        }

        protected virtual void OnCenterFrequencyChanged(FrequencyEventArgs e)
        {
            if (CenterFrequencyChanged != null)
            {
                CenterFrequencyChanged(this, e);
            }
        }

        protected virtual void OnBandwidthChanged(BandwidthEventArgs e)
        {
            if (BandwidthChanged != null)
            {
                BandwidthChanged(this, e);
            }
        }

        private void UpdateFrequency(long f, FrequencyChangeSource source)
        {
            var min = (long)(_displayCenterFrequency - _spectrumWidth / _scale / 2);
            if (f < min)
            {
                f = min;
            }
            var max = (long)(_displayCenterFrequency + _spectrumWidth / _scale / 2);
            if (f > max)
            {
                f = max;
            }

            if (_useSnap)
            {
                f = (f + Math.Sign(f) * _stepSize / 2) / _stepSize * _stepSize;
            }

            if (f != _frequency)
            {
                var args = new FrequencyEventArgs(f, source);
                OnFrequencyChanged(args);
                if (!args.Cancel)
                {
                    _frequency = args.Frequency;
                    _performNeeded = true;
                }
            }
        }

        private void UpdateCenterFrequency(long f)
        {
            if (f < 0)
            {
                f = 0;
            }

            if (_useSnap)
            {
                f = (f + Math.Sign(f) * _stepSize / 2) / _stepSize * _stepSize;
            }

            if (f != _centerFrequency)
            {
                var args = new FrequencyEventArgs(f, FrequencyChangeSource.Scroll);
                OnCenterFrequencyChanged(args);
                if (!args.Cancel)
                {
                    var delta = args.Frequency - _centerFrequency;
                    _displayCenterFrequency += delta;
                    _centerFrequency = args.Frequency;
                    _performNeeded = true;
                }
            }
        }

        private void UpdateBandwidth(int bw)
        {
            bw = 10 * (bw / 10);

            if (bw < 10)
            {
                bw = 10;
            }

            if (bw != _filterBandwidth)
            {
                var args = new BandwidthEventArgs(bw);
                OnBandwidthChanged(args);
                if (!args.Cancel)
                {
                    _filterBandwidth = args.Bandwidth;
                    _performNeeded = true;
                }
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.ChangedButton == MouseButton.Left)
            {
                Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

                System.Windows.Point t = e.GetPosition(this);
                System.Drawing.Point p = new System.Drawing.Point((int)t.X, (int)t.Y);

                var cursorWidth = Math.Max(_filterBandwidth * _xIncrement, 2);
                if (p.X > _lower && p.X < _upper && cursorWidth < ClientRectangle.Width)
                {
                    _oldX = p.X;
                    _oldFrequency = _frequency;
                    _changingFrequency = true;
                }
                else if ((Math.Abs(p.X - _lower + Utils.CursorSnapDistance) <= Utils.CursorSnapDistance &&
                    (_bandType == BandType.Center || _bandType == BandType.Lower))
                    ||
                    (Math.Abs(p.X - _upper - Utils.CursorSnapDistance) <= Utils.CursorSnapDistance &&
                    (_bandType == BandType.Center || _bandType == BandType.Upper)))
                {
                    _oldX = p.X;
                    _oldFilterBandwidth = _filterBandwidth;
                    _changingBandwidth = true;
                }
                else
                {
                    _oldX = p.X;
                    _oldCenterFrequency = _centerFrequency;
                    _changingCenterFrequency = true;
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                UpdateFrequency(_frequency / Utils.RightClickSnapDistance * Utils.RightClickSnapDistance, FrequencyChangeSource.Click);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

            System.Windows.Point t = e.GetPosition(this);
            System.Drawing.Point p = new System.Drawing.Point((int)t.X, (int)t.Y);

            if (_changingCenterFrequency && p.X == _oldX)
            {
                var f = (long)((_oldX - ClientRectangle.Width / 2) * _spectrumWidth / _scale / (ClientRectangle.Width - 2 * AxisMargin) + _displayCenterFrequency);
                UpdateFrequency(f, FrequencyChangeSource.Click);
            }
            _changingCenterFrequency = false;
            _performNeeded = true;
            _changingBandwidth = false;
            _changingFrequency = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Rectangle ClientRectangle = new Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);

            System.Windows.Point t = e.GetPosition(this);
            System.Drawing.Point p = new System.Drawing.Point((int)t.X, (int)t.Y);

            _trackingX = p.X;
            _trackingY = p.Y;
            _trackingFrequency = (long)((p.X - ClientRectangle.Width / 2) * _spectrumWidth / _scale / (ClientRectangle.Width - 2 * AxisMargin) + _displayCenterFrequency);
            if (_useSnap)
            {
                _trackingFrequency = (_trackingFrequency + Math.Sign(_trackingFrequency) * _stepSize / 2) / _stepSize * _stepSize;
            }

            if (_changingFrequency)
            {
                var f = (long)((p.X - _oldX) * _spectrumWidth / _scale / (ClientRectangle.Width - 2 * AxisMargin) + _oldFrequency);
                UpdateFrequency(f, FrequencyChangeSource.Drag);
            }
            else if (_changingCenterFrequency)
            {
                var f = (long)((_oldX - p.X) * _spectrumWidth / _scale / (ClientRectangle.Width - 2 * AxisMargin) + _oldCenterFrequency);
                UpdateCenterFrequency(f);
            }
            else if (_changingBandwidth)
            {
                var bw = 0;
                switch (_bandType)
                {
                    case BandType.Upper:
                        bw = p.X - _oldX;
                        break;

                    case BandType.Lower:
                        bw = _oldX - p.X;
                        break;

                    case BandType.Center:
                        bw = (_oldX > (_lower + _upper) / 2 ? p.X - _oldX : _oldX - p.X) * 2;
                        break;
                }
                bw = (int)(bw * _spectrumWidth / _scale / (ClientRectangle.Width - 2 * AxisMargin) + _oldFilterBandwidth);
                UpdateBandwidth(bw);
            }
            else if ((Math.Abs(p.X - _lower + Utils.CursorSnapDistance) <= Utils.CursorSnapDistance &&
                (_bandType == BandType.Center || _bandType == BandType.Lower))
                ||
                (Math.Abs(p.X - _upper - Utils.CursorSnapDistance) <= Utils.CursorSnapDistance &&
                (_bandType == BandType.Center || _bandType == BandType.Upper)))
            {
                Cursor = Cursors.SizeWE;
            }
            else
            {
                Cursor = Cursors.Arrow;
            }
            _performNeeded = true;
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            Focus();
            base.OnMouseEnter(e);
            _mouseIn = true;
            _performNeeded = true;
            CopyMainBuffer();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _performNeeded = true;
            Perform();
            _mouseIn = false;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            UpdateFrequency(_frequency + _stepSize * Math.Sign(e.Delta), FrequencyChangeSource.Scroll);
        }
    }
}