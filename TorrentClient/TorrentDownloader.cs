using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TorrentClient
{
  public class TorrentDownloader
  {
    private readonly IEnumerable<Peer> _peers;
    private readonly ChannelWriter<Piece> _writer;
    private readonly ChannelReader<Piece> _reader;

    private readonly TorrentFileInfo _info;
    private readonly ConcurrentQueue<RequestItem> _items;

    public TorrentDownloader(IEnumerable<Peer> peers, TorrentFileInfo info,
      ConcurrentQueue<RequestItem> items)
    {
      var channel = Channel.CreateUnbounded<Piece>();
      _writer = channel.Writer;
      _reader = channel.Reader;
      _peers = peers;
      _info = info;
      _items = items;
    }

    public async Task Download()
    {
      var consumerTask = ConsumeAsync();
      var tasks = _peers.Select(peer => Task.Run(() =>
      {
        var cl = new Client(peer, _items, _writer, _info.InfoHash);

        return cl.Process();
      })).ToList();

      tasks.Add(consumerTask);

      await Task.WhenAll(tasks);
    }

    async Task ConsumeAsync()
    {
      await using var fs = new FileStream($"D:\\{_info.Name}", FileMode.Create, FileAccess.Write);
      while (await _reader.WaitToReadAsync())
      {
        if (_reader.TryRead(out var piece))
        {
          var (begin, end) = CalculateBounds(piece.Index, _info);
          fs.Seek(begin, SeekOrigin.Begin);
          fs.Write(piece.Buffer, 0, (int) (end - begin));
        }
      }
    }

    static (long, long) CalculateBounds(int index, TorrentFileInfo info)
    {
      var begin = index * info.PieceSize;
      var end = begin + info.PieceSize;

      if (end > info.Size)
      {
        end = info.Size;
      }

      return (begin, end);
    }
  }
}