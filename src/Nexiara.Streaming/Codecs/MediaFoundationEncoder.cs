using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Nexiara.Abstractions;

namespace Nexiara.Streaming.Codecs
{
    public class MediaFoundationEncoder : Nexiara.Abstractions.IVideoEncoder
    {
        #region Win32 API для захвата кастомного курсора
        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(ref CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        private const int CURSOR_SHOWING = 0x00000001;
        #endregion

        public event Action<byte[]>? OnEncodedPacket;

        private int _targetWidth;
        private int _targetHeight;
        private bool _isInitialized;

        private ImageCodecInfo? _jpegCodec;
        private EncoderParameters? _encoderParams;

        // Zero-Allocation кэш
        private Bitmap? _nativeBitmap;
        private Bitmap? _scaledBitmap;
        private Graphics? _nativeGraphics;
        private Graphics? _scaledGraphics;
        private MemoryStream? _reusableStream;

        public bool IsInitialized => _isInitialized;

        public void Initialize(int width, int height, int targetFps = 60, int bitrateKbps = 6000)
        {
            _targetWidth = width;
            _targetHeight = height;

            _jpegCodec = GetEncoder(ImageFormat.Jpeg);
            _encoderParams = new EncoderParameters(1);
            _encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 40L);

            _scaledBitmap = new Bitmap(_targetWidth, _targetHeight, PixelFormat.Format32bppArgb);
            _scaledGraphics = Graphics.FromImage(_scaledBitmap);

            _scaledGraphics.CompositingQuality = CompositingQuality.HighSpeed;
            _scaledGraphics.InterpolationMode = InterpolationMode.Low;
            _scaledGraphics.SmoothingMode = SmoothingMode.HighSpeed;

            _reusableStream = new MemoryStream(1024 * 512);

            _isInitialized = true;
        }

        public void EncodeFrame(ReadOnlySpan<byte> pixelData, int nativeWidth, int nativeHeight, int stride)
        {
            if (!_isInitialized || pixelData.IsEmpty || _jpegCodec == null || _encoderParams == null || _scaledBitmap == null || _scaledGraphics == null || _reusableStream == null)
                return;

            try
            {
                if (_nativeBitmap == null || _nativeBitmap.Width != nativeWidth || _nativeBitmap.Height != nativeHeight)
                {
                    _nativeGraphics?.Dispose();
                    _nativeBitmap?.Dispose();
                    _nativeBitmap = new Bitmap(nativeWidth, nativeHeight, PixelFormat.Format32bppArgb);
                    _nativeGraphics = Graphics.FromImage(_nativeBitmap);
                }

                var rect = new Rectangle(0, 0, nativeWidth, nativeHeight);
                var bmpData = _nativeBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    unsafe
                    {
                        fixed (byte* srcPtr = pixelData)
                        {
                            int bytesToCopy = Math.Min(pixelData.Length, Math.Abs(stride) * nativeHeight);
                            Buffer.MemoryCopy(srcPtr, (void*)bmpData.Scan0, bmpData.Stride * nativeHeight, bytesToCopy);
                        }
                    }
                }
                finally
                {
                    _nativeBitmap.UnlockBits(bmpData);
                }

                // 🔥 ОТРИСОВКА КАСТОМНОГО КУРСОРА ПРЯМО В КАДР 🔥
                CURSORINFO pci = new CURSORINFO();
                pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                if (GetCursorInfo(ref pci) && pci.flags == CURSOR_SHOWING)
                {
                    try
                    {
                        IntPtr hdc = _nativeGraphics.GetHdc();
                        DrawIcon(hdc, pci.ptScreenPos.x, pci.ptScreenPos.y, pci.hCursor);
                        _nativeGraphics.ReleaseHdc(hdc);
                    }
                    catch { }
                }

                // Масштабируем до 720p и сжимаем
                _scaledGraphics.DrawImage(_nativeBitmap, 0, 0, _targetWidth, _targetHeight);

                _reusableStream.Position = 0;
                _reusableStream.SetLength(0);

                _scaledBitmap.Save(_reusableStream, _jpegCodec, _encoderParams);

                byte[] encodedBytes = _reusableStream.ToArray();
                OnEncodedPacket?.Invoke(encodedBytes);
            }
            catch { }
        }

        private static ImageCodecInfo? GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }

        public void Dispose()
        {
            _encoderParams?.Dispose();
            _nativeGraphics?.Dispose();
            _nativeBitmap?.Dispose();
            _scaledGraphics?.Dispose();
            _scaledBitmap?.Dispose();
            _reusableStream?.Dispose();
            _isInitialized = false;
        }
    }
}