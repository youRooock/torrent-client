using System;
using System.Threading;
using System.Threading.Tasks;
using TorrentClient.Exceptions;
using TorrentClient.Messages;

namespace TorrentClient
{
  public class BittorrentProtocol
  {
    // ReSharper disable once InconsistentNaming
    private const int HANDSHAKE_SIZE = 68;
    private readonly Peer _peer;

    public BittorrentProtocol(Peer peer)
    {
      _peer = peer;
    }

    public void EstablishConnection()
    {
      _peer.Connect();
    }

    public void SendMessage(IMessage message) => _peer.SendBytes(message.Serialize());

    public ResponseMessage ReadMessage()
    {
      var bytes = _peer.ReadBytes();

      return new ResponseMessage(bytes);
    }

    public void PeerHandshake(Handshake handshake)
    {
      _peer.SendBytes(handshake.Bytes);

      var hs = _peer.ReadData(HANDSHAKE_SIZE);
      var handshakeResp = Handshake.Parse(hs);

      if (!handshake.Equals(handshakeResp))
      {
        throw new PeerHandshakeException($"[{_peer.IPEndPoint}] failed to establish handshake");
      }
    }
  }
}