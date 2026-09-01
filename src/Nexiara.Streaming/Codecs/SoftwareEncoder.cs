using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using Nexiara.Abstractions;
using Nexiara.Streaming.Models;

namespace Nexiara.Streaming.Codecs
{
    [SupportedOSPlatform("windows")]
    public class SoftwareEncoder : IVideoEncoder
    {
        public event Action<byte[]>? OnEncodedPacket;

        // Добавили недостающее свойство из интерфейса
        public bool IsInitialized { get; private set; }

        private int _width;
        private int _height;
        private ImageCodecInfo? _jpegEncoder;
        private EncoderParameters _encoderParams;

        public SoftwareEncoder()
        {
            _jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            _encoderParams = new EncoderParameters(1);
            _encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 30L);
        }

        public void Initialize(int width, int height, int targetFps = 60, int bitrateKbps = 6000)
        {
            _width = width;
            _height = height;
            IsInitialized = true; // Отмечаем, что инициализация прошла успешно
        }

        // Совместимость с новым форматом (HardwareEncoder)
        public void EncodeFrame(ReadOnlySpan<byte> pixelData, int width, int height, int stride) { }

        // Совместимость со старым форматом
        public void EncodeFrame(CapturedFrame frame)
        {
            if (!IsInitialized || frame.PixelData == null || frame.PixelData.Length == 0 || _jpegEncoder == null) return;

            try
            {
                using var originalBitmap = new Bitmap(frame.Width, frame.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                var rect = new Rectangle(0, 0, frame.Width, frame.Height);

                var bmpData = originalBitmap.LockBits(rect, ImageLockMode.WriteOnly, originalBitmap.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(frame.PixelData, 0, bmpData.Scan0, frame.PixelData.Length);
                originalBitmap.UnlockBits(bmpData);

                using var resizedBitmap = new Bitmap(originalBitmap, new Size(1280, 720));
                using var ms = new MemoryStream();
                resizedBitmap.Save(ms, _jpegEncoder, _encoderParams);

                byte[] encodedBytes = ms.ToArray();
                OnEncodedPacket?.Invoke(encodedBytes);
            }
            catch { }
        }

        private ImageCodecInfo? GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }

        public void Dispose()
        {
            IsInitialized = false;
            _encoderParams?.Dispose();
        }
    }
}