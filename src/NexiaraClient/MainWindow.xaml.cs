using System;
using System.Windows;
using System.Windows.Media;
using NexiaraClient.Core;

namespace NexiaraClient
{
    public partial class MainWindow : Window
    {
        private NetworkAgent _mobileAgent;
        private WebRtcStreamer _webRtcStreamer;
        private bool _isStreaming = false;
        private AppConfig _config;

        public MainWindow()
        {
            InitializeComponent();

            _config = ConfigManager.LoadOrCreate();
            DeviceUuidBox.Text = $"PIN: {_config.PinCode} | UUID: {_config.DeviceUuid}";

            StartBackgroundAgent();
        }

        private void StartBackgroundAgent()
        {
            try
            {
                _mobileAgent = new NetworkAgent(_config.ServerIp, _config.DeviceUuid);

                StatusText.Text = "Связь с координатором установлена 🟢";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 102));

                _ = _mobileAgent.StartAsync();
            }
            catch (Exception)
            {
                StatusText.Text = "Ошибка подключения 🔴";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void ToggleStreamButton_Click(object sender, RoutedEventArgs e)
        {
            _isStreaming = !_isStreaming;

            if (_isStreaming)
                StartVideoStreaming();
            else
                StopVideoStreaming();
        }

        private void StartVideoStreaming()
        {
            // Передаем секретный токен, чтобы сигналка WebRTC гарантированно пробивала защиту бэкенда
            _webRtcStreamer = new WebRtcStreamer(
                _config.ServerIp,
                _config.DeviceUuid,
                "NEXIARA_SUPER_SECRET_PASSPHRASE_1337"
            );

            _ = _webRtcStreamer.StartAsync();

            ToggleStreamButton.Content = "ОСТАНОВИТЬ ТРАНСЛЯЦИЮ";
            ToggleStreamButton.Background = new SolidColorBrush(Color.FromRgb(200, 50, 50));
            StatusText.Text = "Ожидание WebRTC подключения... ⏳";
        }

        private void StopVideoStreaming()
        {
            _webRtcStreamer?.Stop();
            _webRtcStreamer = null;

            ToggleStreamButton.Content = "ЗАПУСТИТЬ ТРАНСЛЯЦИЮ";
            ToggleStreamButton.Background = new SolidColorBrush(Color.FromRgb(61, 61, 184));
            StatusText.Text = "Трансляция выключена ⚪";
            StatusText.Foreground = new SolidColorBrush(Colors.White);
        }

        protected override void OnClosed(EventArgs e)
        {
            _webRtcStreamer?.Stop();
            _mobileAgent?.Stop();
            base.OnClosed(e);
            Environment.Exit(0);
        }
    }
}