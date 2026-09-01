using System;
using System.Threading.Tasks;

namespace Nexiara.Abstractions
{
    /// <summary>
    /// Единый контракт для всех провайдеров захвата экрана (WGC, DXGI, GDI).
    /// </summary>
    public interface ICaptureProvider : IDisposable
    {
        bool IsRunning { get; }

        /// <summary>
        /// Запускает непрерывный цикл захвата кадров в фоновом режиме.
        /// </summary>
        Task StartCaptureAsync(int targetFps);

        /// <summary>
        /// Останавливает захват экрана.
        /// </summary>
        void StopCapture();

        /// <summary>
        /// Резервный метод для одиночного снимка экрана (совместимость с тестами).
        /// </summary>
        Task<byte[]> TakeScreenshotAsync(int displayIndex = 0, bool fullQuality = false);
    }
}