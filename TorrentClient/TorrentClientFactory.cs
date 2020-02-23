using System;
using System.IO;
using System.Net.Sockets;
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
      var tcpClient = new TcpClient();
      tcpClient.NoDelay = true;
      tcpClient.ReceiveTimeout = 1000;
      tcpClient.SendTimeout = 1000;

      await tcpClient.ConnectAsync(peer.IPEndPoint.Address, peer.IPEndPoint.Port);
      var tcpStream = tcpClient.GetStream();
      tcpStream.ReadTimeout = 1000;
      tcpStream.WriteTimeout = 1000;

      var handshake = Handshake.Create(_infoHash, _peerId);

      await tcpStream.WriteAsync(handshake.Bytes, 0, handshake.Bytes.Length);

      var resp = new byte[68];
      await tcpStream.ReadAsync(resp, 0, resp.Length);
      var handshakeResponse = Handshake.Parse(resp);

      if (!handshake.Equals(handshakeResponse))
      {
        Console.WriteLine("Couldnt establish hasdhakse with " + peer.IPEndPoint.ToString() );
        return null;
      }


      Console.WriteLine("successful handshake with " + peer.IPEndPoint.ToString());

      byte[] messageLength = new byte[4];
      await tcpStream.ReadAsync(messageLength, 0, messageLength.Length);

      var length = BigEndian.ToUint32(messageLength);
      Span<byte> message = new byte[length];

      await tcpStream.ReadAsync(message, 0, bitfield.Length);

      Console.WriteLine(string.Join(" ", message));

      tcpStream.Close();
      tcpClient.Close();

      return new TorrentClient(tcpStream, null);
    }
  }
}
