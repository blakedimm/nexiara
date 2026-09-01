using System;

namespace Nexiara.Streaming.Models
{
    public enum FramePacketType : byte
    {
        Jpeg = 0,
        H264KeyFrame = 1,   // I-Frame (ключевой кадр)
        H264DeltaFrame = 2  // P-Frame (разностный кадр)
    }

    /// <summary>
    /// Компактный пакет сжатого кадра, готовый для трансляции по сети.
    /// </summary>
    public class EncodedPacket
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public int Length { get; set; }
        public FramePacketType Type { get; set; }
        public long Timestamp { get; set; }
        public long SequenceNumber { get; set; }
    }
}