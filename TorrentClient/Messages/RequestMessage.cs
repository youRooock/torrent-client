using System.IO;

namespace TorrentClient.Messages
{
  public class RequestMessage
  {
    private readonly BinaryWriter _writer;
    private const byte MessageID = 6;

    public RequestMessage(BinaryWriter writer)
    {
      _writer = writer;
    }

    public void Send(BlockRequest block)
    {
      _writer.Write(MessageID);
      _writer.Write(block.Index);
      _writer.Write(block.Offset);
      _writer.Write(block.Length);
      _writer.Flush();
    }
  }
}