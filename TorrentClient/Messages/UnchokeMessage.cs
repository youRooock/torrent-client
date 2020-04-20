using System;
using System.IO;
using System.Net;

namespace TorrentClient.Messages
{
  public class UnchokeMessage: IMessage, IResponseMessage
  { 
    public byte Id => (byte) MessageId.Unchoke;
    public byte[] Payload { get; }

    public byte[] Serialize()
    {
      using var ms = new MemoryStream();
      using var bw = new BinaryWriter(ms);

      byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Length));
      bw.Write(bytes);
      bw.Write(Id);
      bw.Flush();

      return ms.ToArray();
    }

    public int Length => 1;
  }
}