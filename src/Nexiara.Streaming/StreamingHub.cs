using System;
using Nexiara.Abstractions;
using Nexiara.Streaming.Capture;
using Nexiara.Streaming.Codecs;
using Nexiara.Streaming.Codecs.Hardware;
using Nexiara.Streaming.Models;
using Nexiara.Streaming.Transport;

namespace Nexiara.Streaming
{
    public class StreamingHub
    {
        private IScreenCapturer? _capturer;
        private IVideoEncoder? _encoder;
        private StreamServer? _server;
        private AbsoluteTouchExecutor? _touchExecutor;

        public void StartStreaming(int width = 1280, int height = 720, int fps = 60, int port = 5005)
        {
            _server = new StreamServer();
            _server.Start(port);

            _encoder = EncoderFactory.CreateBestEncoder();
            _encoder.Initialize(width, height, fps);
            _encoder.OnEncodedPacket += (packet) => _server?.BroadcastFrame(packet);

            _capturer = new DxgiScreenCapturer();

            _capturer.OnFrameCaptured += (frame) =>
            {
                if (_encoder is HardwareVideoEncoder hw)
                    hw.EncodeFrame(frame.PixelData, frame.Width, frame.Height, frame.Width * 4);
                else if (_encoder is SoftwareEncoder sw)
                    sw.EncodeFrame(frame);
            };

            _capturer.StartAsync(fps);

            _touchExecutor = new AbsoluteTouchExecutor();

            Console.WriteLine($"[STREAMING] Запущено! Порт: {port}, Энкодер: {_encoder.GetType().Name}");
        }

        public void ProcessTouch(TouchInput input)
        {
            _touchExecutor?.ProcessTouch(input);
        }

        public void StopStreaming()
        {
            _capturer?.Stop();
            _encoder?.Dispose();
            _server?.Stop();
        }
    }
}