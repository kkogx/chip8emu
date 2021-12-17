using System;

namespace Chip8Emu
{
    public static class PrimitiveExtensions
    {
        public static string ToHex(this byte[] data) => Convert.ToHexString(data);

        public static string ToHex(this byte data) => new byte[] { data }.ToHex();

        public static string ToHex(this ushort data) => data.ToString("X2");
    }
}
