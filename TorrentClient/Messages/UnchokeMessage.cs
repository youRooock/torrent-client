using System.IO;

namespace TorrentClient.Messages
{
  public class UnchokeMessage
  {
    public const byte ID = 1;
    private readonly BinaryWriter _writer;

    public UnchokeMessage(BinaryWriter writer)
    {
      _writer = writer;
    }

    public virtual void Send()
    {
      _writer.Write(ID);
      _writer.Flush();
    }
  }
}