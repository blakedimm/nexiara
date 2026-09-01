using System;
using System.IO;
using System.Text.Json;

namespace NexiaraClient.Core
{
    public class AppConfig
    {
        public string DeviceUuid { get; set; }
        public string PinCode { get; set; }
        // 🔥 Указываем твой публичный домен туннеля по умолчанию
        public string ServerIp { get; set; } = "bot.nexiara.pp.ua";
    }

    public static class ConfigManager
    {
        private const string ConfigFile = "config.json";

        public static AppConfig LoadOrCreate()
        {
            if (File.Exists(ConfigFile))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFile);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (config != null) return config;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка чтения конфига: {ex.Message}. Создаем новый.");
                }
            }
            return CreateNewConfig();
        }

        private static AppConfig CreateNewConfig()
        {
            var rng = new Random();
            string pin = rng.Next(1000, 9999).ToString();
            string uuid = $"NODE-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}-{pin}";

            var newConfig = new AppConfig
            {
                DeviceUuid = uuid,
                PinCode = pin,
                // 🔥 И здесь тоже пишем домен для новых конфигов
                ServerIp = "bot.nexiara.pp.ua"
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(newConfig, options));

            return newConfig;
        }
    }
}