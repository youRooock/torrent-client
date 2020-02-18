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

      int count = 0;
      foreach (var peer in peers)
      {
        try
        {
          var tcp = new TcpClient();
          tcp.Connect(peer.IPEndPoint);
          var tcpStream = tcp.GetStream();


          // var handshake = new Handshake(torrentFileInfo.InfoHash, peerId).Bytes;
          var handshake = Handshake.Create(torrentFileInfo.InfoHash, peerId).Bytes;

          tcpStream.Write(handshake, 0, handshake.Length);

          var resp = new byte[tcp.ReceiveBufferSize];

          var bytes = tcpStream.Read(resp, 0, tcp.ReceiveBufferSize);

          var handshakeResponse = Handshake.Parse(resp);

          var q = handshake.Equals(handshakeResponse);
        }

        catch (Exception e)
        {
          continue;
        }
      }

      Console.WriteLine(peers.Count);
      Console.WriteLine(count);
    }
  }
}
