using TorrentClient.Messages;

namespace TorrentClient
{
  public class MessageHandler
  {
    public MessageHandler()
    {
      
    }

    public IMessage Handle(byte[] arr)
    {
      var message = (MessageId) arr[0] switch
      {
        MessageId.Bitfield => new InterestedMessage(),
        _ => null
      };

      return message;
    }
  }
}