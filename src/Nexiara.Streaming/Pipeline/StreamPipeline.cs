using System;
using System.Threading;
using System.Threading.Tasks;
using Nexiara.Abstractions;
using Nexiara.Streaming.Capture;
using Nexiara.Streaming.Codecs;
using Nexiara.Streaming.Memory;
using Nexiara.Streaming.Models;
using Nexiara.Streaming.Transport;
using IVideoEncoder = Nexiara.Abstractions.IVideoEncoder;

namespace Nexiara.Streaming.Pipeline
{
    public class StreamPipeline : IFramePipeline
    {
        private readonly LockFreeTripleBuffer<RawVideoFrame> _tripleBuffer;
        private ICaptureProvider? _capturer;
        private IVideoEncoder? _encoder;
        private StreamServer? _server;
        private AbsoluteTouchExecutor? _touchExecutor;

        private CancellationTokenSource? _cts;
        private bool _isActive;

        public bool IsActive => _isActive;

        public StreamPipeline()
        {
            _tripleBuffer = new LockFreeTripleBuffer<RawVideoFrame>(
                new RawVideoFrame(),
                new RawVideoFrame(),
                new RawVideoFrame()
            );
        }

        public async Task StartAsync(int width = 1280, int height = 720, int fps = 60)
        {
            if (_isActive) return;

            _isActive = true;
            _cts = new CancellationTokenSource();

            _server = new StreamServer();
            _server.Start(5005);

            // 2. Аппаратный кодировщик кадра (H.264 MFT)
            _encoder = new Nexiara.Streaming.Codecs.Hardware.HardwareVideoEncoder();
            _encoder.Initialize(width, height, fps);
            _encoder.OnEncodedPacket += (packet) => _server.BroadcastFrame(packet);

            _touchExecutor = new AbsoluteTouchExecutor();

            try
            {
                _capturer = new WgcScreenCapturer(_tripleBuffer);
                await _capturer.StartCaptureAsync(fps);
            }
            catch
            {
                _capturer?.Dispose();
                _capturer = new GdiScreenCapturer(_tripleBuffer);
                await _capturer.StartCaptureAsync(fps);
            }

            Task.Run(() => EncodeLoopAsync(fps, _cts.Token));
        }

        public void ProcessTouch(TouchInput input)
        {
            _touchExecutor?.ProcessTouch(input);
        }

        private async Task EncodeLoopAsync(int targetFps, CancellationToken token)
        {
            int intervalMs = Math.Max(1, 1000 / targetFps);

            while (!token.IsCancellationRequested && _isActive)
            {
                var frame = _tripleBuffer.GetLatestReadBuffer();
                if (frame != null && frame.PixelBuffer.Length > 0 && _encoder != null)
                {
                    _encoder.EncodeFrame(frame.PixelBuffer, frame.Width, frame.Height, frame.Stride);
                }

                await Task.Delay(intervalMs, token);
            }
        }

        public void Stop()
        {
            _isActive = false;
            _cts?.Cancel();

            _capturer?.StopCapture();
            _capturer?.Dispose();
            _capturer = null;

            _encoder?.Dispose();
            _encoder = null;

            _server?.Stop();
            _server = null;
        }

        public void Dispose() => Stop();
    }
}