using System;
using System.IO;
using System.Net;

namespace TorrentClient.Utils
{
  public class BigEndianBinaryWriter : BinaryWriter
  {
    public BigEndianBinaryWriter(Stream stream)
      : base(stream)
    {
    }

    public override void Write(short value)
    {
      value = IPAddress.HostToNetworkOrder(value);
      base.Write(value);
    }

    public override void Write(ushort value)
    {
      int networkOrderInt = IPAddress.HostToNetworkOrder((int)value);
      byte[] bytes = BitConverter.GetBytes(networkOrderInt);
      base.Write(bytes, 2, 2);
    }

    public override void Write(int value)
    {
      value = IPAddress.HostToNetworkOrder(value);
      base.Write(value);
    }

    public override void Write(long value)
    {
      value = IPAddress.HostToNetworkOrder(value);
      base.Write(value);
    }
  }
}