using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace TorrentClient
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var torrentFileInfo = new TorrentFileInfo(@"D:\debian.torrent");
      var tracker = new TorrentTracker(torrentFileInfo.Announce);
      var endpoints =
        await tracker.RetrievePeersAsync(torrentFileInfo.InfoHash, PeerId.CreateNew(), torrentFileInfo.Size);

      var peers = endpoints.Select(Peer.Create);

      var queue = new ConcurrentQueue<RequestItem>();

      torrentFileInfo.PieceHashes.Select((hash, index)
          => new RequestItem(index, hash, torrentFileInfo.PieceSize, torrentFileInfo.Size)).ToList()
        .ForEach(r => queue.Enqueue(r));


      var torrentDownloader = new TorrentDownloader(peers, torrentFileInfo, queue);
      await torrentDownloader.Download();

      Console.WriteLine("Downloaded!");
    }
  }
}