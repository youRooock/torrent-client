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
    private readonly MessageHandler _messageHandler;
    public bool IsConnected { get; private set; }

    public BittorrentProtocol(Peer peer, MessageHandler messageHandler)
    {
      _peer = peer;
      _messageHandler = messageHandler;
    }

    public void EstablishConnection()
    {
      _peer.TryConnect();
      IsConnected = true;
    }

    public void SendMessage(IMessage message) => _peer.SendInternal(message.Serialize());

    public ResponseMessage ReadMessage() => _peer.ReadMessage();

    public void ReadMessagesAsync(CancellationToken token)
    {
      Task.Factory.StartNew(() =>
      {
        while (true)
        {
          try
          {
            var messageBytes = _peer.ReadInternal();
            if (messageBytes == null) continue;

            _messageHandler.Handle(new ResponseMessage(messageBytes));
          }

          catch (Exception e)
          {
            Console.WriteLine(e.Message);
            IsConnected = false;
          }
        }
      }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void PeerHandshake(Handshake handshake)
    {
      _peer.SendInternal(handshake.Bytes);

      var hs = _peer.ReadData(HANDSHAKE_SIZE);
      var handshakeResp = Handshake.Parse(hs);

      if (!handshake.Equals(handshakeResp))
      {
        throw new PeerHandshakeException($"[{_peer.IPEndPoint}] failed to establish handshake");
      }
    }
  }
}