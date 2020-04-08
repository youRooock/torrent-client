using System.IO;
using System.Net;

namespace TorrentClient.Utils
{
  public class BigEndianBinaryReader : BinaryReader
  {
    public BigEndianBinaryReader(Stream stream)
      : base(stream)
    {
    }

    public override short ReadInt16()
    {
      short value = base.ReadInt16();
      return IPAddress.NetworkToHostOrder(value);
    }

    public override ushort ReadUInt16()
    {
      ushort value = base.ReadUInt16();
      return (ushort)IPAddress.NetworkToHostOrder((short)value);
    }

    public override int ReadInt32()
    {
      int value = base.ReadInt32();
      return IPAddress.NetworkToHostOrder(value);
    }

    public override long ReadInt64()
    {
      long value = base.ReadInt64();
      return IPAddress.NetworkToHostOrder(value);
    }
  }
}