using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Nexiara.Abstractions;

namespace Nexiara.Platform.Windows.Clipboard
{
    public class WindowsClipboardProvider : IClipboardProvider
    {
        #region Win32 P/Invoke

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        #endregion

        public event Action<string>? OnClipboardChanged;

        public Task<string> GetTextAsync()
        {
            if (!OpenClipboard(IntPtr.Zero)) return Task.FromResult(string.Empty);

            try
            {
                IntPtr hData = GetClipboardData(CF_UNICODETEXT);
                if (hData == IntPtr.Zero) return Task.FromResult(string.Empty);

                IntPtr pData = GlobalLock(hData);
                if (pData == IntPtr.Zero) return Task.FromResult(string.Empty);

                try
                {
                    string text = Marshal.PtrToStringUni(pData) ?? string.Empty;
                    return Task.FromResult(text);
                }
                finally
                {
                    GlobalUnlock(hData);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        public Task SetTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return Task.CompletedTask;
            if (!OpenClipboard(IntPtr.Zero)) return Task.CompletedTask;

            try
            {
                EmptyClipboard();

                int bytesCount = (text.Length + 1) * 2;
                IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytesCount);
                if (hMem == IntPtr.Zero) return Task.CompletedTask;

                IntPtr pData = GlobalLock(hMem);
                if (pData != IntPtr.Zero)
                {
                    try
                    {
                        byte[] bytes = Encoding.Unicode.GetBytes(text + "\0");
                        Marshal.Copy(bytes, 0, pData, bytes.Length);
                    }
                    finally
                    {
                        GlobalUnlock(hMem);
                    }

                    SetClipboardData(CF_UNICODETEXT, hMem);
                    OnClipboardChanged?.Invoke(text);
                }
            }
            finally
            {
                CloseClipboard();
            }

            return Task.CompletedTask;
        }
    }
}