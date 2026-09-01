using System;
using System.Collections.Generic;

namespace Nexiara.Protocol
{
    /// <summary>
    /// Паспорт возможностей устройства, отправляемый при рукопожатии.
    /// </summary>
    public class NodeHandshake
    {
        public string NodeId { get; set; } = Environment.MachineName;
        public string Platform { get; set; } = "Windows"; // "Windows", "Android", "Linux"
        public string AppVersion { get; set; } = "3.0.0";
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// Динамический список возможностей, доступных на ноде прямо сейчас.
        /// </summary>
        public List<string> Capabilities { get; set; } = new();
    }

    /// <summary>
    /// Глобальный реестр стандартных флагов возможностей (Capabilities) системы Nexiara.
    /// </summary>
    public static class NodeCapabilities
    {
        // --- ВВОД И УПРАВЛЕНИЕ ---
        public const string InputMouse = "input.mouse";
        public const string InputKeyboard = "input.keyboard";
        public const string InputTouchpad = "input.touchpad";

        // --- УПРАВЛЕНИЕ ОС ---
        public const string SystemWin32 = "system.win32";
        public const string SystemProcesses = "system.processes";
        public const string SystemClipboard = "system.clipboard";

        // --- АУДИО И МЕДИА ---
        public const string AudioPlayback = "media.audio_playback";
        public const string AudioCapture = "media.audio_capture";
        public const string ScreenCapture = "media.screen_capture";
        public const string ScreenStreamNvenc = "codec.nvenc";
        public const string ScreenStreamCuda = "codec.cuda";

        // --- МЕШ И ФАЙЛЫ ---
        public const string MeshFileSystem = "mesh.filesystem";
        public const string MeshGitSync = "mesh.git_sync";

        // --- СПЕЦИФИКА ANDROID ---
        public const string AndroidAccessibility = "android.accessibility";
        public const string AndroidNotifications = "android.notifications";
        public const string AndroidWirelessAdb = "android.adb_ultra";
    }
}