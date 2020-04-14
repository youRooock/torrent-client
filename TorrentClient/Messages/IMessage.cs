namespace TorrentClient.Messages
{
  public interface IMessage
  {
    byte[] Serialize();
  }
}