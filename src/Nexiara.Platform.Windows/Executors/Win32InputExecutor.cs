using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Nexiara.Abstractions;
using Nexiara.Protocol.Actions;

namespace Nexiara.Platform.Windows.Executors
{
    public class Win32InputExecutor : IInputExecutor
    {
        #region Win32 P/Invoke Declarations

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        #endregion

        private float _accX = 0f;
        private float _accY = 0f;

        public Task MoveMouseAsync(MoveMouseAction action)
        {
            _accX += action.Dx;
            _accY += action.Dy;

            int stepX = (int)_accX;
            int stepY = (int)_accY;

            if (stepX != 0 || stepY != 0)
            {
                _accX -= stepX;
                _accY -= stepY;

                if (GetCursorPos(out POINT cur))
                {
                    bool res = SetCursorPos(cur.X + stepX, cur.Y + stepY);
                    NexiaraLogger.Log("WIN32_MOUSE", $"Результат SetCursorPos={res}. Сдвиг ({stepX}, {stepY}). Позиция: ({cur.X + stepX}, {cur.Y + stepY})");
                }
            }
            return Task.CompletedTask;
        }

        public Task ClickMouseAsync(MouseClickAction action)
        {
            NexiaraLogger.Log("WIN32_CLICK", $"Клик: {action.Button}");
            uint flags = action.Button.ToLower() == "right"
                ? (MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP)
                : (MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP);

            mouse_event(flags, 0, 0, 0, UIntPtr.Zero);
            return Task.CompletedTask;
        }

        public Task ScrollMouseAsync(MouseScrollAction action)
        {
            NexiaraLogger.Log("WIN32_SCROLL", $"Скролл: {action.DeltaY}");
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)action.DeltaY, UIntPtr.Zero);
            return Task.CompletedTask;
        }

        public Task SendKeyPressAsync(KeyPressAction action)
        {
            NexiaraLogger.Log("WIN32_KEY", $"Нажатие клавиши: {action.Key}");
            byte vk = action.Key.ToLower() switch
            {
                "win" => 0x5B,
                "backspace" => 0x08,
                "enter" => 0x0D,
                "vol_up" => 0xAF,
                "vol_down" => 0xAE,
                "media_play" => 0xB3,
                "c" => 0x43,
                "v" => 0x56,
                "tab" => 0x09,
                _ => 0
            };

            if (vk == 0) return Task.CompletedTask;

            if (action.Control) keybd_event(0x11, 0, 0, UIntPtr.Zero);
            if (action.Alt) keybd_event(0x12, 0, 0, UIntPtr.Zero);

            keybd_event(vk, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            if (action.Alt) keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (action.Control) keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            return Task.CompletedTask;
        }

        public Task TypeTextAsync(TypeTextAction action)
        {
            NexiaraLogger.Log("WIN32_TEXT", $"Печать текста: {action.Text}");
            if (string.IsNullOrEmpty(action.Text)) return Task.CompletedTask;

            foreach (char c in action.Text)
            {
                INPUT[] inputs = new INPUT[2];

                // Нажатие (Key Down)
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].u.ki.wVk = 0;
                inputs[0].u.ki.wScan = (ushort)c;
                inputs[0].u.ki.dwFlags = KEYEVENTF_UNICODE;
                inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

                // Отпускание (Key Up)
                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].u.ki.wVk = 0;
                inputs[1].u.ki.wScan = (ushort)c;
                inputs[1].u.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
                inputs[1].u.ki.dwExtraInfo = IntPtr.Zero;

                SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
            }

            return Task.CompletedTask;
        }

        public Task ProcessTouchpadGestureAsync(TouchpadGestureAction action) => Task.CompletedTask;
    }
}