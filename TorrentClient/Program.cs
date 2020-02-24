using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TorrentClient.Utils;

namespace TorrentClient
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var torrentFileInfo = new TorrentFileInfo(@"D:\ubuntu.torrent");
      var peerId = new byte[20];
      new Random().NextBytes(peerId);
      var tracker = new TorrentTracker(torrentFileInfo.Announce);
      var peers = await tracker.RetrievePeersAsync(torrentFileInfo.InfoHash, peerId, torrentFileInfo.Size);
      var availablePeers = new List<Peer>();

      var sw = Stopwatch.StartNew();

      var torrectFactory = new TorrentClientFactory(torrentFileInfo.InfoHash, peerId);

      var tasks = peers.Select(async peer =>
      {
        try
        {
          using var cl = await torrectFactory.ConnectAsync(peer);

          var bf = cl == null ? default : await cl.GetBitmapField();
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
  }
}
