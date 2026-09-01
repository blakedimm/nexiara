using System;

namespace Nexiara.Streaming.Codecs.ColorSpace
{
    public static unsafe class Nv12Converter
    {
        public static void ConvertArgbToNv12(ReadOnlySpan<byte> argb, byte[] nv12, int width, int height, int stride)
        {
            fixed (byte* argbPtr = argb)
            fixed (byte* nv12Ptr = nv12)
            {
                byte* yPlane = nv12Ptr;
                byte* uvPlane = nv12Ptr + (width * height);

                for (int y = 0; y < height; y++)
                {
                    byte* row = argbPtr + y * stride;
                    int yIndex = y * width;
                    int uvRowIndex = (y / 2) * width; // U и V чередуются

                    for (int x = 0; x < width; x++)
                    {
                        int b = row[x * 4];
                        int g = row[x * 4 + 1];
                        int r = row[x * 4 + 2];

                        // Быстрое вычисление яркости (Y)
                        int yVal = ((66 * r + 129 * g + 25 * b + 128) >> 8) + 16;
                        yPlane[yIndex + x] = (byte)Math.Clamp(yVal, 0, 255);

                        // Субдискретизация цвета (UV) - берем каждый второй пиксель
                        if (y % 2 == 0 && x % 2 == 0)
                        {
                            int uVal = ((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128;
                            int vVal = ((112 * r - 94 * g - 18 * b + 128) >> 8) + 128;

                            int uvIndex = uvRowIndex + x;
                            uvPlane[uvIndex] = (byte)Math.Clamp(uVal, 0, 255);
                            uvPlane[uvIndex + 1] = (byte)Math.Clamp(vVal, 0, 255);
                        }
                    }
                }
            }
        }
    }
}
