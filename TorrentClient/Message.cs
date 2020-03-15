using System;
using System.Linq;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class Message
  {
    private const int MessageSizeLength = 4; // 4 bytes for determining length

    public byte[] Payload { get; set; }
    public MessageId Id { get; set; }

    public Message()
    {

    }

    public Message(byte[] arr)
    {
      Id = (MessageId)arr[0];
      Payload = arr[1..^0];
    }

    public byte[] Serialize()
    {
      Span<byte> buffer = new byte[MessageSizeLength + TotalMessageSize];
      BigEndian.PutUint32(buffer, TotalMessageSize);
      buffer[4] = (byte)Id;

      return buffer.ToArray();
    }

    private int TotalMessageSize => Payload?.Length ?? 0 + 1; // extra byte for id
  }

  public enum MessageId
  {
    Choke,
    Unchoke,
    Interested,
    NotInterested,
    Have,
    Bitfield,
    Request,
    Piece,
    Cancel
  }
}
