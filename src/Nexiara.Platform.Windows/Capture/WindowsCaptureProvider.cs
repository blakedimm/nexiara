using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Nexiara.Abstractions;

namespace Nexiara.Platform.Windows.Capture
{
    public class WindowsCaptureProvider : ICaptureProvider
    {
        #region Win32 GDI P/Invoke

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        #endregion

        private CancellationTokenSource? _streamCts;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public Task<byte[]> TakeScreenshotAsync(int displayIndex = 0, bool fullQuality = false)
        {
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);

            byte[] dummyFrame = new byte[1024];
            return Task.FromResult(dummyFrame);
        }

        public Task StartCaptureAsync(int targetFps)
        {
            if (_isRunning) return Task.CompletedTask;
            _isRunning = true;
            _streamCts = new CancellationTokenSource();

            return Task.CompletedTask;
        }

        public void StopCapture()
        {
            _isRunning = false;
            _streamCts?.Cancel();
        }

        public void Dispose() => StopCapture();
    }
}