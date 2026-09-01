using System;
using System.Diagnostics;

namespace Nexiara.Platform.Windows
{
    public static class FirewallManager
    {
        public static void EnsureFirewallRules()
        {
            try
            {
                // Добавляем правило для TCP сокета (управление + видеопоток)
                RunNetsh("advfirewall firewall add rule name=\"Nexiara Core Server\" dir=in action=allow protocol=TCP localport=5000 enable=yes profile=any");

                // Добавляем правило для UDP маяка (автопоиск ПК в Wi-Fi)
                RunNetsh("advfirewall firewall add rule name=\"Nexiara Discovery Beacon\" dir=in action=allow protocol=UDP localport=5001 enable=yes profile=any");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Firewall] Ошибка настройки брандмауэра: {ex.Message}");
            }
        }

        private static void RunNetsh(string args)
        {
            var psi = new ProcessStartInfo("netsh", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();
        }
    }
}