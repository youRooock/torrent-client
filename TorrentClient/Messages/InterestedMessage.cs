using System.IO;

namespace TorrentClient.Messages
{
  public class InterestedMessage
  {
    public const byte ID = (int) MessageId.Have;
    private readonly BinaryWriter _writer;

    public InterestedMessage(BinaryWriter writer)
    {
      _writer = writer;
    }

    public void Send()
    {
      _writer.Write(ID);
      _writer.Flush();
    }
  }
}