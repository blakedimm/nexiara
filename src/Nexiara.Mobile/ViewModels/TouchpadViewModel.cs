using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Nexiara.Mobile.Services;

namespace Nexiara.Mobile.ViewModels
{
    public class TouchpadViewModel : INotifyPropertyChanged
    {
        private readonly NetworkClientService _networkClient = NetworkClientService.Instance;
        private string _pcName = "ПК";
        private bool _isConnected;

        public string PcName
        {
            get => _pcName;
            set { _pcName = value; OnPropertyChanged(); }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); }
        }

        public ICommand ClickCommand { get; }
        public ICommand LockCommand { get; }

        public TouchpadViewModel()
        {
            ClickCommand = new Command<string>(async (btn) => await SendClickAsync(btn));
            LockCommand = new Command(async () => await SendLockAsync());
        }

        public async Task<bool> InitializeConnectionAsync(string ipAddress, string pcName)
        {
            PcName = pcName;
            IsConnected = await _networkClient.ConnectAsync(ipAddress);
            return IsConnected;
        }

        public async Task SendMoveAsync(float dx, float dy)
        {
            if (!IsConnected) return;
            await _networkClient.SendActionAsync(new { type = "move", dx, dy });
        }

        public async Task SendClickAsync(string? button)
        {
            if (!IsConnected || string.IsNullOrEmpty(button)) return;
            await _networkClient.SendActionAsync(new { type = "click", button });
        }

        public async Task SendLockAsync()
        {
            if (!IsConnected) return;
            await _networkClient.SendActionAsync(new { type = "lock" });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}