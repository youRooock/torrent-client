using System;
using System.IO;
using System.Net;
using TorrentClient.Utils;

namespace TorrentClient.Messages
{
  public class HaveMessage
  {
    private readonly long _index;
    public const byte Id = (int) MessageId.Have;

    public HaveMessage(long index)
    {
      _index = index;
    }

    public byte[] Serialize()
    {
      using var ms = new MemoryStream();
      using var bw = new BinaryWriter(ms);

      byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)_index));
      byte[] lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length + 1));
      bw.Write(lengthBytes);
      bw.Write(Id);
      bw.Write(bytes);
      bw.Flush();

      return ms.ToArray();
    }

    public int Length => 1;
  }
}