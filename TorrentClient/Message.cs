using System;
namespace TorrentClient
{
  public class Message
  {
    public byte[] Payload { get; set; }
    public MessageId Id { get; set; }
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
