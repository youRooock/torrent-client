using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TorrentClient.Events;
using TorrentClient.Messages;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class Client
  {
    private readonly BittorrentProtocol _bittorrent;
    private readonly MessageHandler _messageHandler;
    private readonly ConcurrentQueue<RequestItem> _items;
    private readonly ChannelWriter<Piece> _channelWriter;
    private bool _choked = true;
    private Bitfield _bitfield;

    public Client(
      Peer peer,
      ConcurrentQueue<RequestItem> items,
      ChannelWriter<Piece> channelWriter,
      byte[] infoHash)
    {
      _items = items;
      _channelWriter = channelWriter;
      _messageHandler = new MessageHandler();
      _bittorrent = new BittorrentProtocol(peer, _messageHandler);
      _bittorrent.EstablishConnection();
      _bittorrent.PeerHandshake(Handshake.Create(infoHash, PeerId.CreateNew()));
      _messageHandler.OnBitfieldReceived += @event => { _bitfield = @event.Bitfield; };
      _messageHandler.OnChokeReceived += () => { _choked = true; };
      _messageHandler.OnUnchokeReceived += () => { _choked = false; };
    }

    public void Process()
    {
      var cts = new CancellationTokenSource();

      _bittorrent.SendMessage(new UnchokeMessage());
      _bittorrent.SendMessage(new InterestedMessage());

      _bittorrent.ReadMessagesAsync(cts.Token);

      while (!_items.IsEmpty)
      {
        var piece = new Piece();

        if (!_items.TryDequeue(out var item)) continue;
        if (!_bitfield.HasPiece(item.Index))
        {
          _items.Enqueue(item);
          continue;
        }

        _messageHandler.OnPieceReceived += PieceCallback;

        while (piece.Downloaded < item.Length)
        {
          if (_choked) continue;

          while (piece.Requested < item.Length)
          {
            if (item.Length - piece.Requested < piece.BlockSize)
            {
              piece.BlockSize = item.Length - piece.Requested;
            }

            _bittorrent.SendMessage(new RequestMessage(new PieceBlock(item.Index, piece.Requested, piece.BlockSize)));
            piece.Requested += piece.BlockSize;
          }
        }

        _messageHandler.OnPieceReceived -= PieceCallback;

        void PieceCallback(PieceEventArgs e)
        {
          var n = ParsePiece(item.Index, piece.Buffer, e.Payload);
          piece.Downloaded =+ n;
        }
      }
    }

    // ToDo: handle cases
    static int ParsePiece(int index, byte[] buffer, byte[] payload)
    {
      if (payload.Length < 8)
      {
      }

      var parsedIndex = BigEndian.ToUint32(payload[0..4]);
      if (parsedIndex != index)
      {
      }

      var begin = BigEndian.ToUint32(payload[4..8]);
      if (begin >= buffer.Length)
      {
      }

      var data = payload[8..];
      if (begin + data.Length > buffer.Length)
      {
      }

      Array.Copy(data, 0, buffer, begin, data.Length);
      return data.Length;
    }
  }
  }