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
      if (bytes.Length != 4) throw new ArgumentException("Byte array should have 2 elements");

      return bytes[3] | bytes[2] << 8 | bytes[1] << 16 | bytes[0] << 24;
    }
  }
}
