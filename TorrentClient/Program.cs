using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TorrentClient
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var torrentFileInfo = new TorrentFileInfo(@"/Users/yourock/Documents/ubuntu-19.10-desktop-amd64.iso.torrent");
      var peerId = new byte[20];
      new Random().NextBytes(peerId);
      var tracker = new TorrentTracker(torrentFileInfo.Announce);
      var peers = await tracker.RetrievePeersAsync(torrentFileInfo.InfoHash, peerId, torrentFileInfo.Size);
      var availablePeers = new List<Peer>();

      var sw = Stopwatch.StartNew();

      var torrectFactory = new TorrentClientFactory(torrentFileInfo.InfoHash, peerId);

      var queue = new ConcurrentQueue<WorkItem>();

      var items = torrentFileInfo.PieceHashes.Select((hash, index) =>
      {
        return new WorkItem(index, hash, CalculatePieceLength(torrentFileInfo, index));
      });

      foreach (var item in items)
      {
        queue.Enqueue(item);
      }

      var tasks = peers.Select(async peer =>
      {
        try
        {
          queue.TryDequeue(out var item);
          var cl = await torrectFactory.ConnectAsync(peer);

          if (cl == null)
          {
            return;
          }

          //var bf = await cl.GetBitmapField();

          //await cl.SendUnchoke();
          //await cl.SendInterested();

          //if (!bf.HasPiece(item.Index))
          //{
          //  queue.Enqueue(item);
          //}

          //cl.TryDownload(item.Index, item.Length);
          cl.Dispose();
        }

        catch (Exception e)
        {
          Console.WriteLine(e.ToString());
        }
      });

      await Task.WhenAll(tasks);

      sw.Stop();

      Console.WriteLine(sw.ElapsedMilliseconds);
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

  public class WorkItem
  {
    public WorkItem(int index, byte[] hash, long length)
    {
      Index = index;
      Hash = hash;
      Length = length;
    }
    public int Index { get; }
    public byte[] Hash { get; }
    public long Length { get; }
  }
}
