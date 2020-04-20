using System;
using System.IO;
using System.Net;

namespace TorrentClient.Messages
{
  public class HaveMessage: IMessage
  {
    private readonly long _index;
    public byte Id => (int) MessageId.Have;

    public HaveMessage(long index)
    {
      _index = index;
    }

    public byte[] Payload { get; }

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
  }
}