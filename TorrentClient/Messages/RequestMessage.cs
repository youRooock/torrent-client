using System;
using System.IO;
using System.Net;

namespace TorrentClient.Messages
{
  public class RequestMessage: IMessage
  {
    private readonly BlockRequest _block;
    private const byte Id = (byte) MessageId.Request;

    public RequestMessage(BlockRequest block)
    {
      _block = block;
    }

    public byte[] Serialize()
    {
      using var ms = new MemoryStream();
      using var bw = new BinaryWriter(ms);

      var index = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)_block.Index));
      var offset = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)_block.Offset));
      var length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)_block.Length));
      var messageLength =
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder(index.Length + offset.Length + length.Length + 1));

      bw.Write(messageLength);
      bw.Write(Id);
      bw.Write(index);
      bw.Write(offset);
      bw.Write(length);
      bw.Flush();

      return ms.ToArray();
    }
  }
}