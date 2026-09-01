using System;
using System.Runtime.InteropServices;
using Nexiara.Streaming.Models;

namespace Nexiara.Streaming.Capture
{
    public class AbsoluteTouchExecutor
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
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

        private const uint INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        public void ProcessTouch(TouchInput input)
        {
            // Переводим 0.0..1.0 в абсолютную систему Win32 (0..65535)
            int absX = (int)(Math.Clamp(input.NormalizedX, 0.0f, 1.0f) * 65535);
            int absY = (int)(Math.Clamp(input.NormalizedY, 0.0f, 1.0f) * 65535);

            uint flags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;

            switch (input.Action)
            {
                case TouchAction.Down:
                    flags |= MOUSEEVENTF_LEFTDOWN;
                    break;
                case TouchAction.Up:
                    flags |= MOUSEEVENTF_LEFTUP;
                    break;
                case TouchAction.Tap:
                    ExecuteClick(absX, absY, MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP);
                    return;
                case TouchAction.RightTap:
                    ExecuteClick(absX, absY, MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP);
                    return;
            }

            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = absX;
            inputs[0].mi.dy = absY;
            inputs[0].mi.dwFlags = flags;

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void ExecuteClick(int absX, int absY, uint clickFlags)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = absX;
            inputs[0].mi.dy = absY;
            inputs[0].mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE | clickFlags;

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}