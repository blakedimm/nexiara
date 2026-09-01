using System;
using System.Threading.Tasks;

namespace Nexiara.Abstractions
{
    /// <summary>
    /// Контракт сквозного конвейера обработки и трансляции кадров.
    /// </summary>
    public interface IFramePipeline : IDisposable
    {
        bool IsActive { get; }

        /// <summary>
        /// Запускает всю цепочку: Capture -> TripleBuffer -> Encoder -> Network.
        /// </summary>
        Task StartAsync(int width = 1280, int height = 720, int fps = 60);

        /// <summary>
        /// Безопасно останавливает конвейер и освобождает ресурсы GPU/памяти.
        /// </summary>
        void Stop();
    }
}