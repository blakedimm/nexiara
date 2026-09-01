using System;

namespace Nexiara.Streaming.Models
{
    /// <summary>
    /// Контейнер сырых пиксельных данных кадра для захвата без перевыделения памяти.
    /// </summary>
    public class RawVideoFrame
    {
        public byte[] PixelBuffer { get; set; } = Array.Empty<byte>();
        public IntPtr NativePointer { get; set; } = IntPtr.Zero;
        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }
        public long Timestamp { get; set; }

        // Метаданные курсора
        public bool IsCursorVisible { get; set; }
        public int CursorX { get; set; }
        public int CursorY { get; set; }

        public void EnsureBufferSize(int requiredBytes)
        {
            if (PixelBuffer.Length < requiredBytes)
            {
                PixelBuffer = new byte[requiredBytes];
            }
        }
    }
}