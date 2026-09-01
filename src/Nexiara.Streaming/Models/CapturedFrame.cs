using System;

namespace Nexiara.Streaming.Models
{
    public class CapturedFrame
    {
        public byte[] PixelData { get; set; } = Array.Empty<byte>();
        public int Width { get; set; }
        public int Height { get; set; }
        public long TimestampBuffer { get; set; }

        // Метаданные курсора (для отрисовки на клиенте или оверлея)
        public bool IsCursorVisible { get; set; }
        public int CursorX { get; set; }
        public int CursorY { get; set; }
    }
}