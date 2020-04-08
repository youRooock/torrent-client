using System.IO;
using TorrentClient.Utils;

namespace TorrentClient.Messages
{
  public class HaveMessage
  {
    public const byte ID = (int) MessageId.Have;
    private readonly BinaryWriter _writer;

    public HaveMessage(BinaryWriter writer)
    {
      _writer = writer;
    }

    public void Send(long index)
    {
      var payload = new byte[4];
      BigEndian.PutUint32(payload, index);
      _writer.Write(ID);
      _writer.Write(payload);
      _writer.Flush();
    }
  }
}