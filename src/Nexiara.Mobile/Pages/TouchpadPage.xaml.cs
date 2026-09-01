using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;
using Nexiara.Mobile.Services;

namespace Nexiara.Mobile.Pages
{
    public partial class TouchpadPage : ContentPage
    {
        private double _lastX;
        private double _lastY;
        private double _lastScrollY;
        private float _sensitivity = 1.8f;
        private bool _isFullScreen = false;

        private ClientWebSocket? _videoSocket;
        private CancellationTokenSource? _videoCts;

        // --- ПЕРЕМЕННЫЕ ДВИЖКА УМНЫХ ЖЕСТОВ ---
        private DateTime _lastTapTime = DateTime.MinValue;
        private bool _isDragging = false;
        private CancellationTokenSource? _singleTapCts;

        public TouchpadPage()
        {
            InitializeComponent();
            SetupGestureEngine();
            SetupScrollStrip();
        }

        public async void Initialize(string ipAddress, string pcName)
        {
            Preferences.Default.Set("last_target_ip", ipAddress);
            Preferences.Default.Set("last_target_pc_name", pcName);

            if (BindingContext is ViewModels.TouchpadViewModel vm)
            {
                await vm.InitializeConnectionAsync(ipAddress, pcName);
            }

            _ = StartVideoStreamAsync(ipAddress);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            string savedIp = Preferences.Default.Get("last_target_ip", string.Empty);
            string savedName = Preferences.Default.Get("last_target_pc_name", "Мой ПК");

            if (!string.IsNullOrEmpty(savedIp) && (_videoSocket == null || _videoSocket.State != WebSocketState.Open))
            {
                Initialize(savedIp, savedName);
            }
        }

        private async Task StartVideoStreamAsync(string ipAddress)
        {
            _videoCts?.Cancel();
            _videoCts = new CancellationTokenSource();

            MainThread.BeginInvokeOnMainThread(() => StatusLabel.IsVisible = false);

            while (!_videoCts.Token.IsCancellationRequested)
            {
                _videoSocket?.Dispose();
                _videoSocket = new ClientWebSocket();
                _videoSocket.Options.SetBuffer(1024 * 512, 1024 * 512);
                _videoSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);

                try
                {
                    await _videoSocket.ConnectAsync(new Uri($"ws://{ipAddress}:5005/stream/"), _videoCts.Token);

                    var buffer = new byte[1024 * 128];

                    while (_videoSocket.State == WebSocketState.Open && !_videoCts.Token.IsCancellationRequested)
                    {
                        using var ms = new MemoryStream();
                        WebSocketReceiveResult result;

                        do
                        {
                            result = await _videoSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _videoCts.Token);
                            if (result.MessageType == WebSocketMessageType.Close) break;

                            ms.Write(buffer, 0, result.Count);
                        }
                        while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close) break;

                        byte[] frameData = ms.ToArray();

                        if (frameData.Length > 0)
                        {
#if ANDROID
                            Nexiara.Mobile.Platforms.Android.Video.VideoSurfaceHandler.Decoder.FeedPacket(frameData);
#endif
                        }
                    }
                }
                catch { }

                if (!_videoCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, _videoCts.Token);
                }
            }
        }

        // --- ДВИЖОК СЕНСОРНЫХ ЖЕСТОВ (SINGLE TAP, DOUBLE TAP, TAP & DRAG) ---
        private void SetupGestureEngine()
        {
            var panGesture = new PanGestureRecognizer();

            panGesture.PanUpdated += async (s, e) =>
            {
                if (BindingContext is not ViewModels.TouchpadViewModel vm) return;

                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        _lastX = 0;
                        _lastY = 0;

                        var now = DateTime.UtcNow;
                        double timeSinceLastTap = (now - _lastTapTime).TotalMilliseconds;

                        // Если второй тап произошел быстро (в течение 280мс) -> включаем режим Drag (зажатие объекта)
                        if (timeSinceLastTap < 280)
                        {
                            _isDragging = true;
                            _singleTapCts?.Cancel(); // Отменяем одиночный клик
                            HapticService.Click();
                            // Отправляем зажатие ЛКМ на ПК
                            _ = NetworkClientService.Instance.SendActionAsync(new { type = "click", button = "down" });
                        }
                        break;

                    case GestureStatus.Running:
                        float dx = (float)(e.TotalX - _lastX) * _sensitivity;
                        float dy = (float)(e.TotalY - _lastY) * _sensitivity;
                        _lastX = e.TotalX;
                        _lastY = e.TotalY;

                        if (Math.Abs(dx) > 0.05f || Math.Abs(dy) > 0.05f)
                        {
                            _ = vm.SendMoveAsync(dx, dy);
                        }
                        break;

                    case GestureStatus.Completed:
                    case GestureStatus.Canceled:
                        if (_isDragging)
                        {
                            _isDragging = false;
                            // Отпускаем ЛКМ
                            _ = NetworkClientService.Instance.SendActionAsync(new { type = "click", button = "up" });
                        }
                        _lastX = 0;
                        _lastY = 0;
                        break;
                }
            };

            VideoContainer.GestureRecognizers.Add(panGesture);

            // Обработка одиночных и двойных тапов по экрану
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) =>
            {
                var now = DateTime.UtcNow;
                double timeSpan = (now - _lastTapTime).TotalMilliseconds;
                _lastTapTime = now;

                if (timeSpan < 280)
                {
                    // ✌ ДВОЙНОЙ ТАП -> ПРАВЫЙ КЛИК (ПКМ)
                    _singleTapCts?.Cancel();
                    HapticService.Click();
                    await NetworkClientService.Instance.SendActionAsync(new { type = "click", button = "right" });
                }
                else
                {
                    // ☝ ОДИНОЧНЫЙ ТАП -> ЛЕВАЯ КНОПКА МЫШИ (ЛКМ)
                    _singleTapCts?.Cancel();
                    _singleTapCts = new CancellationTokenSource();

                    try
                    {
                        // Небольшая задержка, чтобы убедиться, что это не двойной тап
                        await Task.Delay(200, _singleTapCts.Token);
                        if (!_isDragging)
                        {
                            HapticService.Click();
                            await NetworkClientService.Instance.SendActionAsync(new { type = "click", button = "left" });
                        }
                    }
                    catch (TaskCanceledException) { }
                }
            };

            VideoContainer.GestureRecognizers.Add(tapGesture);
        }

        private void SetupScrollStrip()
        {
            var scrollPan = new PanGestureRecognizer();
            scrollPan.PanUpdated += (s, e) =>
            {
                if (BindingContext is not ViewModels.TouchpadViewModel vm) return;

                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        _lastScrollY = 0;
                        break;

                    case GestureStatus.Running:
                        double dy = e.TotalY - _lastScrollY;
                        _lastScrollY = e.TotalY;

                        if (Math.Abs(dy) > 1.0)
                        {
                            int scrollAmount = dy > 0 ? -1 : 1;
                            _ = NetworkClientService.Instance.SendActionAsync(new { type = "scroll", dy = scrollAmount });
                        }
                        break;
                }
            };
            ScrollStripContainer.GestureRecognizers.Add(scrollPan);
        }

        private void ToggleSettings_Clicked(object sender, EventArgs e)
        {
            SettingsPanel.IsVisible = !SettingsPanel.IsVisible;
        }

        private void SensitivitySlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            _sensitivity = (float)e.NewValue;
            if (SensitivityLabel != null)
            {
                SensitivityLabel.Text = $"{_sensitivity:F1}x";
            }
        }

        private async void QuickKey_Clicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string param)
            {
                HapticService.Click();
                if (param == "alt_tab")
                    await NetworkClientService.Instance.SendActionAsync(new { type = "key_combo", combo = "alt_tab" });
                else if (param == "task_mgr")
                    await NetworkClientService.Instance.SendActionAsync(new { type = "key_combo", combo = "ctrl_shift_esc" });
                else
                    await NetworkClientService.Instance.SendActionAsync(new { type = "type_text", text = param });
            }
        }

        protected override void OnDisappearing()
        {
            _videoCts?.Cancel();
            _videoSocket?.Dispose();
            base.OnDisappearing();
        }
    }
}