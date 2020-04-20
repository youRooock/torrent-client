namespace TorrentClient.Messages
{
  public class ResponseMessage
  {
    public MessageId Id { get; }
    public byte[] Payload { get; }

    public ResponseMessage(byte[] arr)
    {
      Id = (MessageId) arr[0];
      Payload = arr[1..];
    }
  }
}