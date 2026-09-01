using System;
using Android.Media;
using Android.Views;
using Java.Nio;

namespace Nexiara.Mobile.Platforms.Android.Video
{
    public class H264HardwareDecoder : IDisposable
    {
        private MediaCodec? _codec;
        private bool _isConfigured;

        public bool IsReady => _isConfigured;

        public void Initialize(Surface surface, int width = 1280, int height = 720)
        {
            try
            {
                // Формат видеопотока: H.264 / AVC
                var format = MediaFormat.CreateVideoFormat(MediaFormat.MimetypeVideoAvc, width, height);

                // Создаем нативный аппаратный декодер Android
                _codec = MediaCodec.CreateDecoderByType(MediaFormat.MimetypeVideoAvc);

                // Привязываем выхлоп декодера НАПРЯМУЮ к нативному SurfaceView
                _codec.Configure(format, surface, null, MediaCodecConfigFlags.None);
                _codec.Start();
                _isConfigured = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[H264HardwareDecoder] Ошибка инициализации: {ex.Message}");
                _isConfigured = false;
            }
        }

        public void FeedPacket(byte[] data)
        {
            if (!_isConfigured || _codec == null || data == null || data.Length == 0)
                return;

            try
            {
                // 1. Запрашиваем свободный входной буфер у декодера (таймаут 5 мс)
                int inputIndex = _codec.DequeueInputBuffer(5000);
                if (inputIndex >= 0)
                {
                    ByteBuffer? inputBuffer = _codec.GetInputBuffer(inputIndex);
                    if (inputBuffer != null)
                    {
                        inputBuffer.Clear();
                        inputBuffer.Put(data);

                        // Засылаем кадр в видеочип
                        _codec.QueueInputBuffer(inputIndex, 0, data.Length, 0, 0);
                    }
                }

                // 2. Отправляем раскодированные кадры в VRAM для моментального отображения
                var bufferInfo = new MediaCodec.BufferInfo();
                int outputIndex = _codec.DequeueOutputBuffer(bufferInfo, 0);

                while (outputIndex >= 0)
                {
                    // render: true мгновенно рисует готовый пиксельный буфер на дисплей
                    _codec.ReleaseOutputBuffer(outputIndex, true);
                    outputIndex = _codec.DequeueOutputBuffer(bufferInfo, 0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[H264HardwareDecoder] Ошибка декодирования: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _isConfigured = false;
            try
            {
                _codec?.Stop();
                _codec?.Release();
                _codec?.Dispose();
                _codec = null;
            }
            catch { }
        }
    }
}