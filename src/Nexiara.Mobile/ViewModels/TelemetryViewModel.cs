using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Nexiara.Mobile.Services;

namespace Nexiara.Mobile.ViewModels
{
    public class TelemetryViewModel : INotifyPropertyChanged
    {
        private readonly NetworkClientService _networkClient = NetworkClientService.Instance;

        private string _activeWindow = "Ожидание данных...";
        private string _ramUsage = "— MB";
        private string _clipboardText = "—";

        public string ActiveWindow
        {
            get => _activeWindow;
            set { _activeWindow = value; OnPropertyChanged(); }
        }

        public string RamUsage
        {
            get => _ramUsage;
            set { _ramUsage = value; OnPropertyChanged(); }
        }

        public string ClipboardText
        {
            get => _clipboardText;
            set { _clipboardText = value; OnPropertyChanged(); }
        }

        public ICommand SyncClipboardCommand { get; }

        public TelemetryViewModel()
        {
            SyncClipboardCommand = new Command(async () => await SyncClipboardAsync());
            _networkClient.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("active_window", out var winProp))
                    ActiveWindow = winProp.GetString() ?? "Неизвестно";

                if (root.TryGetProperty("ram_usage", out var ramProp))
                    RamUsage = $"{ramProp.GetInt64()} MB";
            }
            catch { }
        }

        public async Task SyncClipboardAsync()
        {
            HapticService.Click();
            if (Clipboard.Default.HasText)
            {
                string text = await Clipboard.Default.GetTextAsync() ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    ClipboardText = text;
                    await _networkClient.SendActionAsync(new { type = "set_clipboard", text });
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}