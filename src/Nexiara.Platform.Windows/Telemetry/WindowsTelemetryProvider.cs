using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Nexiara.Abstractions;

namespace Nexiara.Platform.Windows.Telemetry
{
    public class WindowsTelemetryProvider : ITelemetryProvider
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public Task<SystemTelemetry> GetCurrentTelemetryAsync()
        {
            // 1. Читаем заголовок активного окна через Win32 API
            var sb = new StringBuilder(256);
            IntPtr handle = GetForegroundWindow();
            string activeTitle = "Рабочий стол";

            if (GetWindowText(handle, sb, 256) > 0)
            {
                string title = sb.ToString();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    activeTitle = title;
                }
            }

            // 2. Считываем оперативную память текущего процесса/системы
            long memoryUsageMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);

            var telemetry = new SystemTelemetry(
                CpuLoad: "Active",
                GpuLoad: "N/A",
                RamUsage: $"{memoryUsageMb} MB",
                ActiveWindowTitle: activeTitle,
                BatteryPercentage: 100
            );

            return Task.FromResult(telemetry);
        }
    }
}