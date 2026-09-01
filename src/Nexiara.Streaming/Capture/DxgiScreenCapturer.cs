using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Nexiara.Streaming.Models;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using SharpGen.Runtime;

namespace Nexiara.Streaming.Capture
{
    public class DxgiScreenCapturer : IScreenCapturer
    {
        public event Action<CapturedFrame>? OnFrameCaptured;

        private ID3D11Device? _d3dDevice;
        private ID3D11DeviceContext? _d3dContext;
        private IDXGIOutputDuplication? _deskDupl;
        private ID3D11Texture2D? _stagingTexture;

        private CancellationTokenSource? _cts;
        private int _width;
        private int _height;

        public bool IsRunning { get; private set; }

        public Task StartAsync(int targetFps = 60)
        {
            if (IsRunning) return Task.CompletedTask;

            InitDxgi();
            IsRunning = true;
            _cts = new CancellationTokenSource();

            int frameIntervalMs = Math.Max(1, 1000 / targetFps);
            Task.Run(() => CaptureLoopAsync(frameIntervalMs, _cts.Token));

            return Task.CompletedTask;
        }

        private void InitDxgi()
        {
            if (DXGI.CreateDXGIFactory1(out IDXGIFactory1? factory).Failure || factory == null)
            {
                throw new Exception("Не удалось создать DXGI Factory.");
            }

            using (factory)
            {
                uint adapterIndex = 0;
                // Сканируем все GPU в системе (Intel iGPU и NVIDIA RTX 4050)
                while (factory.EnumAdapters1(adapterIndex, out IDXGIAdapter1? adapter).Success && adapter != null)
                {
                    using (adapter)
                    {
                        uint outputIndex = 0;
                        // Сканируем дисплеи, подключенные к текущему GPU
                        while (adapter.EnumOutputs(outputIndex, out IDXGIOutput? output).Success && output != null)
                        {
                            using (output)
                            {
                                try
                                {
                                    // 🔥 Явно указываем типы out-параметров, чтобы C# однозначно определил метод 🔥
                                    var result = D3D11.D3D11CreateDevice(
                                        adapter,
                                        DriverType.Unknown,
                                        DeviceCreationFlags.None,
                                        new[] { FeatureLevel.Level_11_0 },
                                        out ID3D11Device? d3dDevice,
                                        out ID3D11DeviceContext? d3dContext
                                    );

                                    if (result.Failure || d3dDevice == null || d3dContext == null)
                                        continue;

                                    using var output1 = output.QueryInterface<IDXGIOutput1>();
                                    var deskDupl = output1.DuplicateOutput(d3dDevice);

                                    // Нашли рабочую видеокарту, к которой подключен активный экран
                                    _d3dDevice = d3dDevice;
                                    _d3dContext = d3dContext;
                                    _deskDupl = deskDupl;

                                    var desc = output.Description;
                                    _width = desc.DesktopCoordinates.Right - desc.DesktopCoordinates.Left;
                                    _height = desc.DesktopCoordinates.Bottom - desc.DesktopCoordinates.Top;

                                    var stagingDesc = new Texture2DDescription
                                    {
                                        Width = (uint)_width,
                                        Height = (uint)_height,
                                        MipLevels = 1,
                                        ArraySize = 1,
                                        Format = Format.B8G8R8A8_UNorm,
                                        SampleDescription = new SampleDescription(1, 0),
                                        Usage = ResourceUsage.Staging,
                                        BindFlags = BindFlags.None,
                                        CPUAccessFlags = CpuAccessFlags.Read,
                                        MiscFlags = ResourceOptionFlags.None
                                    };

                                    _stagingTexture = _d3dDevice.CreateTexture2D(stagingDesc);
                                    return; // Захват успешно инициализирован!
                                }
                                catch
                                {
                                    // Если видеокарта не связана с экраном напрямую — тихо переходим к следующей
                                }
                            }
                            outputIndex++;
                        }
                    }
                    adapterIndex++;
                }
            }

            throw new Exception("Не удалось найти совместимый GPU и монитор для DXGI Desktop Duplication.");
        }

        private async Task CaptureLoopAsync(int intervalMs, CancellationToken token)
        {
            while (!token.IsCancellationRequested && IsRunning)
            {
                try
                {
                    GrabFrame();
                }
                catch (SharpGenException)
                {
                    Reinitialize();
                }
                catch { }

                await Task.Delay(intervalMs, token);
            }
        }

        private void GrabFrame()
        {
            if (_deskDupl == null || _stagingTexture == null || _d3dContext == null) return;

            var resInfo = _deskDupl.AcquireNextFrame(10, out OutduplFrameInfo frameInfo, out IDXGIResource? desktopResource);
            if (resInfo.Failure || desktopResource == null) return;

            using (desktopResource)
            {
                using var desktopTexture = desktopResource.QueryInterface<ID3D11Texture2D>();
                _d3dContext.CopyResource(_stagingTexture, desktopTexture);
            }

            _deskDupl.ReleaseFrame();

            MappedSubresource mapped = _d3dContext.Map(_stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
            byte[] rawPixels = new byte[_width * _height * 4];

            Marshal.Copy(mapped.DataPointer, rawPixels, 0, rawPixels.Length);
            _d3dContext.Unmap(_stagingTexture, 0);

            var capturedFrame = new CapturedFrame
            {
                PixelData = rawPixels,
                Width = _width,
                Height = _height,
                TimestampBuffer = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IsCursorVisible = frameInfo.PointerPosition.Visible,
                CursorX = frameInfo.PointerPosition.Position.X,
                CursorY = frameInfo.PointerPosition.Position.Y
            };

            OnFrameCaptured?.Invoke(capturedFrame);
        }

        private void Reinitialize()
        {
            StopInternal();
            Thread.Sleep(200);
            try { InitDxgi(); } catch { }
        }

        public void Stop()
        {
            IsRunning = false;
            _cts?.Cancel();
            StopInternal();
        }

        private void StopInternal()
        {
            _stagingTexture?.Dispose();
            _stagingTexture = null;
            _deskDupl?.Dispose();
            _deskDupl = null;
            _d3dContext?.Dispose();
            _d3dContext = null;
            _d3dDevice?.Dispose();
            _d3dDevice = null;
        }

        public void Dispose() => Stop();
    }
}