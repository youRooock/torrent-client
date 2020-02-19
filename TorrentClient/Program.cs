using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace TorrentClient
{
  class Program
  {
    static void Main(string[] args)
    {
      var torrentFileInfo = new TorrentFileInfo("/Users/yourock/Downloads/ubuntu-19.10-desktop-amd64.iso.torrent");
      var peerId = new byte[20];

      new Random().NextBytes(peerId);

      var tracker = new TorrentTracker(torrentFileInfo.Announce);

      var peers = tracker.RetrievePeersAsync(torrentFileInfo.InfoHash, peerId, torrentFileInfo.Size).Result;

      var availablePeers = new List<Peer>();

      foreach (var peer in peers)
      {
        try
        {
          var tcp = new TcpClient();
          var result = tcp.BeginConnect(peer.IPEndPoint.Address, peer.IPEndPoint.Port, null, null);


          var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

          if (!success)
          {
            Console.WriteLine("Failed to connect.");
          }

          // we have connected
          tcp.EndConnect(result);


          var tcpStream = tcp.GetStream();
          var handshake = Handshake.Create(torrentFileInfo.InfoHash, peerId);

          tcpStream.Write(handshake.Bytes, 0, handshake.Bytes.Length);

          var resp = new byte[tcp.ReceiveBufferSize];

          var bytes = tcpStream.Read(resp, 0, tcp.ReceiveBufferSize);

          var handshakeResponse = Handshake.Parse(resp);

          if (handshake.Equals(handshakeResponse)) availablePeers.Add(peer);
        }

        catch (Exception e)
        {
          continue;
        }
      }
    }
  }
}
