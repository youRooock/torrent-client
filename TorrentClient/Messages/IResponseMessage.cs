namespace TorrentClient.Messages
{
  public interface IResponseMessage
  {
    public byte[] Payload { get; }
    public byte Id { get; }
  }
}