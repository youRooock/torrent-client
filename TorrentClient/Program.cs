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
      var torrentFileInfo = new TorrentFileInfo("/Users/yourock/Downloads/ubuntu-19.10-desktop-amd64.iso.torrent");
      var peerId = new byte[20];
      new Random().NextBytes(peerId);
      var tracker = new TorrentTracker(torrentFileInfo.Announce);
      var peers = await tracker.RetrievePeersAsync(torrentFileInfo.InfoHash, peerId, torrentFileInfo.Size);
      var availablePeers = new List<Peer>();

      var sw = Stopwatch.StartNew();

      var torrectFactory = new TorrentClientFactory(torrentFileInfo.InfoHash, peerId);

      //Parallel.ForEach(peers, new ParallelOptions { MaxDegreeOfParallelism = 15 }, peer =>
      // {
      //   using var cl = torrectFactory.ConnectAsync(peer).Result;

      //   var bf = cl == null ? default : cl.GetBitmapField().Result;
      // });

      var tasks = peers.Select(peer =>
      {
        return Task.Run(async () =>
        {
          using var cl = await torrectFactory.ConnectAsync(peer);

          var bf = cl == null ? default : await cl.GetBitmapField();
        });
      });

      try
      {
        await Task.WhenAll(tasks);
      }

      catch (Exception e)
      {
        Console.WriteLine(e.ToString());
      }

      sw.Stop();

      Console.WriteLine(sw.ElapsedMilliseconds);
    }
  }
}
