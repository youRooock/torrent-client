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

    public TorrentClient ConnectAsync(Peer peer)
    {
      var connection = new Connection(peer.IPEndPoint);
      var handshake = Handshake.Create(_infoHash, _peerId);

      connection.Write(handshake.Bytes);

      var resp = new byte[68];

      connection.Read(resp);
      var handshakeResponse = Handshake.Parse(resp);

      if (!handshake.Equals(handshakeResponse))
      {
        Console.WriteLine("couldn't establish handshake with " + peer.IPEndPoint);
        return null;
      }

      Console.WriteLine($"{peer.IPEndPoint} successful handshake with");

      return new TorrentClient(connection);
    }
  }
}
