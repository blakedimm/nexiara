using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Nexiara.Abstractions;
using Nexiara.Streaming.Memory;
using Nexiara.Streaming.Models;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Nexiara.Streaming.Capture
{
    public class WgcScreenCapturer : ICaptureProvider
    {
        private readonly LockFreeTripleBuffer<RawVideoFrame> _tripleBuffer;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        private ID3D11Device? _device;
        private ID3D11DeviceContext? _context;
        private IDXGIOutputDuplication? _duplication;
        private ID3D11Texture2D? _stagingTexture;

        private int _width;
        private int _height;

        public bool IsRunning => _isRunning;

        public WgcScreenCapturer(LockFreeTripleBuffer<RawVideoFrame> tripleBuffer)
        {
            _tripleBuffer = tripleBuffer;
        }

        public Task StartCaptureAsync(int targetFps)
        {
            if (_isRunning) return Task.CompletedTask;

            InitializeDxgi();
            _isRunning = true;
            _cts = new CancellationTokenSource();

            int intervalMs = Math.Max(1, 1000 / targetFps);
            Task.Run(() => CaptureLoopAsync(intervalMs, _cts.Token));

            return Task.CompletedTask;
        }

        private void InitializeDxgi()
        {
            if (DXGI.CreateDXGIFactory1(out IDXGIFactory1? factory).Failure || factory == null)
                throw new Exception("Не удалось создать DXGI Factory.");

            using (factory)
            {
                uint adapterIndex = 0;
                while (factory.EnumAdapters1(adapterIndex, out IDXGIAdapter1? adapter).Success && adapter != null)
                {
                    using (adapter)
                    {
                        uint outputIndex = 0;
                        while (adapter.EnumOutputs(outputIndex, out IDXGIOutput? output).Success && output != null)
                        {
                            using (output)
                            {
                                try
                                {
                                    var result = D3D11.D3D11CreateDevice(
                                        adapter,
                                        DriverType.Unknown,
                                        DeviceCreationFlags.None,
                                        new[] { FeatureLevel.Level_11_0 },
                                        out ID3D11Device? d3dDevice,
                                        out ID3D11DeviceContext? d3dContext
                                    );

                                    if (result.Failure || d3dDevice == null || d3dContext == null) continue;

                                    using var output1 = output.QueryInterface<IDXGIOutput1>();
                                    var deskDupl = output1.DuplicateOutput(d3dDevice);

                                    _device = d3dDevice;
                                    _context = d3dContext;
                                    _duplication = deskDupl;

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

                                    _stagingTexture = _device.CreateTexture2D(stagingDesc);
                                    return;
                                }
                                catch { }
                            }
                            outputIndex++;
                        }
                    }
                    adapterIndex++;
                }
            }

            throw new Exception("Не удалось инициализировать устройство графического захвата.");
        }

        private async Task CaptureLoopAsync(int intervalMs, CancellationToken token)
        {
            while (!token.IsCancellationRequested && _isRunning)
            {
                try
                {
                    GrabFrame();
                }
                catch { }

                await Task.Delay(intervalMs, token);
            }
        }

        private void GrabFrame()
        {
            if (_duplication == null || _stagingTexture == null || _context == null) return;

            var res = _duplication.AcquireNextFrame(10, out OutduplFrameInfo frameInfo, out IDXGIResource? desktopResource);
            if (res.Failure || desktopResource == null) return;

            using (desktopResource)
            {
                using var desktopTexture = desktopResource.QueryInterface<ID3D11Texture2D>();
                _context.CopyResource(_stagingTexture, desktopTexture);
            }

            _duplication.ReleaseFrame();

            MappedSubresource mapped = _context.Map(_stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
            try
            {
                var writeFrame = _tripleBuffer.GetWriteBuffer();
                int bufferSize = _width * _height * 4;
                writeFrame.EnsureBufferSize(bufferSize);

                Marshal.Copy(mapped.DataPointer, writeFrame.PixelBuffer, 0, bufferSize);
                writeFrame.Width = _width;
                writeFrame.Height = _height;
                writeFrame.Stride = _width * 4;
                writeFrame.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                writeFrame.IsCursorVisible = frameInfo.PointerPosition.Visible;
                writeFrame.CursorX = frameInfo.PointerPosition.Position.X;
                writeFrame.CursorY = frameInfo.PointerPosition.Position.Y;

                _tripleBuffer.CommitWrite();
            }
            finally
            {
                _context.Unmap(_stagingTexture, 0);
            }
        }

        public Task<byte[]> TakeScreenshotAsync(int displayIndex = 0, bool fullQuality = false)
        {
            var current = _tripleBuffer.GetLatestReadBuffer();
            if (current != null && current.PixelBuffer.Length > 0)
            {
                byte[] copy = new byte[current.PixelBuffer.Length];
                Array.Copy(current.PixelBuffer, copy, copy.Length);
                return Task.FromResult(copy);
            }
            return Task.FromResult(Array.Empty<byte>());
        }

        public void StopCapture()
        {
            _isRunning = false;
            _cts?.Cancel();

            _stagingTexture?.Dispose();
            _stagingTexture = null;
            _duplication?.Dispose();
            _duplication = null;
            _context?.Dispose();
            _context = null;
            _device?.Dispose();
            _device = null;
        }

        public void Dispose() => StopCapture();
    }
}