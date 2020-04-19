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

    public BittorrentProtocol(Peer peer, MessageHandler messageHandler)
    {
      _peer = peer;
      _messageHandler = messageHandler;
    }

    public void EstablishConnection()
    {
      _peer.TryConnect();
     // PeerHandshake();
    }

    public void SendMessage(IMessage message) => _peer.SendInternal(message.Serialize());

    public Message GetMessage() => _peer.ReadMessage();

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

            var message = ParseMessage(messageBytes);

            _messageHandler.Handle(message);
          }

          catch (Exception e)
          {
            
          }
        }
      }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private IResponseMessage ParseMessage(byte[] arr)
    {
      
      // handle all messages
      var message = arr[0] switch
      {
        (int)MessageId.Bitfield => new BitfieldMessage(arr),
        (int)MessageId.Choke => new BitfieldMessage(arr),
        (int)MessageId.Unchoke => new BitfieldMessage(arr),
        (int)MessageId.Have => new BitfieldMessage(arr),
        (int)MessageId.Piece => new BitfieldMessage(arr),
        _ => throw new ArgumentException("Unknown message type")
      };

      return message;
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