using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Nexiara.Mobile.Services;

namespace Nexiara.Mobile.ViewModels
{
    public class KeyboardViewModel : INotifyPropertyChanged
    {
        private readonly NetworkClientService _networkClient = NetworkClientService.Instance;
        private string _inputText = string.Empty;

        public string InputText
        {
            get => _inputText;
            set { _inputText = value; OnPropertyChanged(); }
        }

        public ICommand SendTextCommand { get; }
        public ICommand KeyComboCommand { get; }

        public KeyboardViewModel()
        {
            SendTextCommand = new Command(async () => await SendTextAsync());
            KeyComboCommand = new Command<string>(async (key) => await SendKeyComboAsync(key));
        }

        public async Task SendTextAsync()
        {
            if (string.IsNullOrEmpty(InputText)) return;
            HapticService.Click();
            await _networkClient.SendActionAsync(new { type = "type_text", text = InputText });
            InputText = string.Empty;
        }

        public async Task SendKeyComboAsync(string? combo)
        {
            if (string.IsNullOrEmpty(combo)) return;
            HapticService.Click();
            await _networkClient.SendActionAsync(new { type = "key_combo", combo });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}