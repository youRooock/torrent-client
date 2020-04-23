using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace TorrentClient
{
  class Program
  {
    /// <summary>
    /// ToDo:
    /// 1. Limit concurrency (clients)
    /// 2. Signal when all pieces are done, so to close consumer
    /// 3. Cancel newly created client when all pieces are done
    /// 4. Not create new client when pieces are done
    /// 5. Check integrity of piece
    /// </summary>

    static async Task Main(string[] args)
    {
      var torrentFileInfo = new TorrentFileInfo(@"D:\ubuntu.torrent");
      var tracker = new TorrentTracker(torrentFileInfo.Announce);
      var endpoints =
        await tracker.RetrievePeersAsync(torrentFileInfo.InfoHash, PeerId.CreateNew(), torrentFileInfo.Size);

      var peers = endpoints.Select(Peer.Create);

      var queue = new ConcurrentQueue<RequestItem>();

      torrentFileInfo.PieceHashes.Select((hash, index)
          => new RequestItem(index, hash, CalculatePieceLength(torrentFileInfo, index))).ToList()
        .ForEach(r => queue.Enqueue(r));


      var torrentDownloader = new TorrentDownloader(peers, torrentFileInfo, queue);
      await torrentDownloader.Download();
    }

    static long CalculatePieceLength(TorrentFileInfo info, int index)
    {
      var begin = index * info.PieceSize;
      var end = begin + info.PieceSize;

      if (end > info.Size)
      {
        end = info.Size;
      }

      return end - begin;
    }
  }
}