namespace TorrentClient.Messages
{
  public interface IMessage
  {
    public byte Id { get; }
    public byte[] Payload { get; }
    byte[] Serialize();
  }
}