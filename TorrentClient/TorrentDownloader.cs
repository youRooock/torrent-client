using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;

namespace TorrentClient
{
  public class TorrentDownloader
  {
    private readonly IEnumerable<Peer> _peers;
    private readonly ChannelWriter<Piece> _writer;
    private readonly TorrentFileInfo _info;
    private readonly ConcurrentQueue<RequestItem> _items;

    public TorrentDownloader(IEnumerable<Peer> peers, ChannelWriter<Piece> writer, TorrentFileInfo info,
      ConcurrentQueue<RequestItem> items)
    {
      _peers = peers;
      _writer = writer;
      _info = info;
      _items = items;
    }

    public void Download()
    {
      foreach (var peer in _peers)
      {
        try
        {
          var client = new Client(peer, _items, _writer, _info.InfoHash);

          client.Process();
        }

        catch (Exception)
        {
          
        }
      }
    }
  }
}