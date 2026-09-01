using System;
using System.Threading.Tasks;
using Nexiara.Streaming.Models;

namespace Nexiara.Streaming.Capture
{
    public interface IScreenCapturer : IDisposable
    {
        event Action<CapturedFrame>? OnFrameCaptured;

        Task StartAsync(int targetFps = 60);
        void Stop();
        bool IsRunning { get; }
    }
}