using System.Threading;
using System.Threading.Tasks;
using TorrentClient.Exceptions;

namespace TorrentClient
{
  public class BittorrentProtocol
  {
    // ReSharper disable once InconsistentNaming
    private const int HANDSHAKE_SIZE = 68;
    private readonly Peer _peer;
    private readonly MessageHandler _messageHandler;

    public BittorrentProtocol(Peer peer, MessageHandler messageHandler)
    {
      _peer = peer;
      _messageHandler = messageHandler;
    }

    public void EstablishConnection()
    {
      _peer.TryConnect();
      PeerHandshake();
    }


    public Task ReadMessagesAsync(CancellationToken token)
    {
      return Task.Factory.StartNew(() =>
      {
        while (true)
        {
          var messageBytes = _peer.ReadInternal();
          if (messageBytes == null) continue;

          _messageHandler.Handle(messageBytes);
        }
      }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void PeerHandshake(Handshake handshake)
    {
      _peer.SendInternal(handshake.Bytes);

      var hs = _peer.ReadData(HANDSHAKE_SIZE);
      var handshake2 = Handshake.Parse(hs);

      if (!handshake.Equals(handshake2))
      {
        throw new PeerHandshakeException($"[{_peer.IPEndPoint}] failed to establish handshake");
      }
    }
  }
}