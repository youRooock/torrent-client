using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class TorrentClientFactory
  {
    private readonly byte[] _peerId;
    private readonly byte[] _infoHash;

    public TorrentClientFactory(byte[] infoHash, byte[] peerId)
    {
      _infoHash = infoHash;
      _peerId = peerId;
    }

    public async Task<TorrentClient> ConnectAsync(Peer peer)
    {
      var tcpClient = new TcpClient {NoDelay = true, ReceiveTimeout = 3000, SendTimeout = 3000};

      var result = tcpClient.BeginConnect(peer.IPEndPoint.Address, peer.IPEndPoint.Port, null, null);
      var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
      
      if (!success)
      {
        Console.WriteLine($"Couldn't connect {peer.IPEndPoint}");
        return null;
      }

      tcpClient.EndConnect(result);
      
      // await tcpClient.ConnectAsync(peer.IPEndPoint.Address, peer.IPEndPoint.Port);
      var tcpStream = tcpClient.GetStream();

      tcpStream.ReadTimeout = 3000;
      tcpStream.WriteTimeout = 3000;

      var handshake = Handshake.Create(_infoHash, _peerId);

      await tcpStream.WriteAsync(handshake.Bytes, 0, handshake.Bytes.Length);

      var resp = new byte[68];
      await tcpStream.ReadAsync(resp, 0, resp.Length);
      var handshakeResponse = Handshake.Parse(resp);

      if (!handshake.Equals(handshakeResponse))
      {
        Console.WriteLine("couldn't establish handshake with " + peer.IPEndPoint);
        return null;
      }

      Console.WriteLine("successful handshake with " + peer.IPEndPoint);

      return new TorrentClient(tcpClient);
    }
  }
}
