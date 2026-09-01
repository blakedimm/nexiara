using System;

namespace Nexiara.Abstractions
{
    /// <summary>
    /// Единый контракт для кодировщиков видео (MediaFoundation H.264, Software JPEG).
    /// </summary>
    public interface IVideoEncoder : IDisposable
    {
        /// <summary>
        /// Событие готовности сжатого пакета (NAL-юнит H.264 или JPEG байты) для отправки в сеть.
        /// </summary>
        event Action<byte[]>? OnEncodedPacket;

        bool IsInitialized { get; }

        /// <summary>
        /// Инициализация параметров видеопотока.
        /// </summary>
        void Initialize(int width, int height, int targetFps = 60, int bitrateKbps = 6000);

        /// <summary>
        /// Передача кадра на сжатие через сырой буфер памяти без лишних выделений GC.
        /// </summary>
        void EncodeFrame(ReadOnlySpan<byte> pixelData, int width, int height, int stride);
    }
}