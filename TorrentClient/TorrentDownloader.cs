using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using TorrentClient.Exceptions;
using TorrentClient.Extensions;

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

      await _peers.ForEachAsync(10, DownloadInternal);

      _writer.Complete();

     await consumerTask;
    }

    private Task DownloadInternal(Peer peer)
    {
      if (!_items.IsEmpty)
      {
        try
        {
          var c = new Client(peer, _items, _writer, _info.InfoHash);

          return c.Process();
        }

        catch (PeerHandshakeException e)
        {
          Console.WriteLine(e.Message);
        }
        catch (PeerCommunicationException e)
        {
          Console.WriteLine(e.Message);
        }
      }

      return Task.CompletedTask;
    }


    private async Task ConsumeAsync()
    {
      var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

      await using var fs = new FileStream(Path.Combine(directory, _info.Name), FileMode.Create, FileAccess.Write);
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