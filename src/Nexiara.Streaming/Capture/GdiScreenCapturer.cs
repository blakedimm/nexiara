using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Nexiara.Abstractions;
using Nexiara.Streaming.Memory;
using Nexiara.Streaming.Models;

namespace Nexiara.Streaming.Capture
{
    public class GdiScreenCapturer : ICaptureProvider
    {
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        private readonly LockFreeTripleBuffer<RawVideoFrame> _tripleBuffer;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public GdiScreenCapturer(LockFreeTripleBuffer<RawVideoFrame> tripleBuffer)
        {
            _tripleBuffer = tripleBuffer;
        }

        public Task StartCaptureAsync(int targetFps)
        {
            if (_isRunning) return Task.CompletedTask;
            _isRunning = true;
            _cts = new CancellationTokenSource();

            int intervalMs = Math.Max(10, 1000 / targetFps);
            Task.Run(() => CaptureLoopAsync(intervalMs, _cts.Token));

            return Task.CompletedTask;
        }

        private async Task CaptureLoopAsync(int intervalMs, CancellationToken token)
        {
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            if (screenWidth <= 0) screenWidth = 1920;
            if (screenHeight <= 0) screenHeight = 1080;

            while (!token.IsCancellationRequested && _isRunning)
            {
                try
                {
                    using var bmp = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
                    }

                    var rect = new Rectangle(0, 0, screenWidth, screenHeight);
                    var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                    try
                    {
                        var writeFrame = _tripleBuffer.GetWriteBuffer();
                        int byteCount = Math.Abs(bmpData.Stride) * screenHeight;
                        writeFrame.EnsureBufferSize(byteCount);

                        Marshal.Copy(bmpData.Scan0, writeFrame.PixelBuffer, 0, byteCount);
                        writeFrame.Width = screenWidth;
                        writeFrame.Height = screenHeight;
                        writeFrame.Stride = bmpData.Stride;
                        writeFrame.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        _tripleBuffer.CommitWrite();
                    }
                    finally
                    {
                        bmp.UnlockBits(bmpData);
                    }
                }
                catch { }

                await Task.Delay(intervalMs, token);
            }
        }

        public Task<byte[]> TakeScreenshotAsync(int displayIndex = 0, bool fullQuality = false)
        {
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            if (screenWidth <= 0) screenWidth = 1920;
            if (screenHeight <= 0) screenHeight = 1080;

            using var bmp = new Bitmap(screenWidth, screenHeight);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return Task.FromResult(ms.ToArray());
        }

        public void StopCapture()
        {
            _isRunning = false;
            _cts?.Cancel();
        }

        public void Dispose() => StopCapture();
    }
}