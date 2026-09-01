using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Nexiara.Mobile.Services;

namespace Nexiara.Mobile.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DiscoveryService _discoveryService = new();

        private string _statusMessage = "Нажмите для поиска ПК в домашней Wi-Fi сети";
        private string _targetPcName = "ПК не найден";
        private string _targetIpAddress = "—";
        private bool _isScanning;
        private bool _isPcFound;

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string TargetPcName
        {
            get => _targetPcName;
            set { _targetPcName = value; OnPropertyChanged(); }
        }

        public string TargetIpAddress
        {
            get => _targetIpAddress;
            set { _targetIpAddress = value; OnPropertyChanged(); }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                _isScanning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotScanning));
            }
        }

        public bool IsNotScanning => !IsScanning;

        public bool IsPcFound
        {
            get => _isPcFound;
            set
            {
                _isPcFound = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public string StatusColor => IsPcFound ? "#00E5FF" : "#FF5252";

        public ICommand ScanCommand { get; }

        public MainViewModel()
        {
            ScanCommand = new Command(async () => await ScanForPcAsync());
        }

        public async Task ScanForPcAsync()
        {
            if (IsScanning) return;

            IsScanning = true;
            IsPcFound = false;
            StatusMessage = "Сканирование локальной сети...";

            try
            {
                var pc = await _discoveryService.FindPcAsync();

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (pc != null)
                    {
                        TargetPcName = pc.Name;
                        TargetIpAddress = $"{pc.IpAddress}:{pc.Port}";
                        IsPcFound = true;
                        StatusMessage = "Узел Nexiara найден! Подключение...";

                        // Сохраняем данные подключения
                        Preferences.Default.Set("last_target_ip", pc.IpAddress);
                        Preferences.Default.Set("last_target_pc_name", pc.Name);

                        // Безопасное переключение на существующую вкладку ТАЧПАД
                        if (Shell.Current != null)
                        {
                            await Shell.Current.GoToAsync("//TouchpadPage");
                        }
                    }
                    else
                    {
                        TargetPcName = "Не найден";
                        TargetIpAddress = "—";
                        IsPcFound = false;
                        StatusMessage = "ПК не ответил. Проверьте Wi-Fi и запущен ли Nexiara на ПК.";
                    }

                    IsScanning = false;
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TargetPcName = "Ошибка";
                    TargetIpAddress = "—";
                    IsPcFound = false;
                    StatusMessage = $"Ошибка поиска: {ex.Message}";
                    IsScanning = false;
                });
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}