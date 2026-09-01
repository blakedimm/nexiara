using System.Runtime.Versioning;
using Nexiara.Abstractions;
using Nexiara.Streaming.Codecs.Hardware;

namespace Nexiara.Streaming.Codecs
{
    [SupportedOSPlatform("windows")]
    public static class EncoderFactory
    {
        public static IVideoEncoder CreateBestEncoder()
        {
            // Напрямую отдаём наш мощный аппаратный H.264 MFT кодировщик!
            // Он сам задействует NVENC чип без лишних вопросов.
            return new HardwareVideoEncoder();
        }
    }
}