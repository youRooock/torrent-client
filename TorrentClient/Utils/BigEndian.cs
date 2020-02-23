using System;

namespace TorrentClient.Utils
{
  public static class BigEndian
  {
    public static int ToUint16(ReadOnlySpan<byte> bytes)
    {
      if (bytes.Length != 2) throw new ArgumentException("Byte array should have 2 elements");

      return bytes[1] | bytes[0] << 8;
    }
    public static int ToUint32(ReadOnlySpan<byte> bytes)
    {
      if (bytes.Length != 4) throw new ArgumentException("Byte array should have 4 elements");

      return bytes[3] | bytes[2] << 8 | bytes[1] << 16 | bytes[0] << 24;
    }

    public static void PutUint32(Span<byte> bytes, int length)
    {
      bytes[0] = (byte)(length >> 24);
      bytes[1] = (byte)(length >> 16);
      bytes[2] = (byte)(length >> 8);
      bytes[3] = (byte)length;
    }
  }
}
