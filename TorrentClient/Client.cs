using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using TorrentClient.Events;
using TorrentClient.Exceptions;
using TorrentClient.Messages;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class Client
  {
    private const int MAX_CHOKED_COUNT = 10;
    private const int MAX_EMPTY_BITFIELD_COUNT = 10;
    private readonly BittorrentProtocol _bittorrent;
    private readonly MessageHandler _messageHandler;
    private readonly ConcurrentQueue<RequestItem> _items;
    private readonly ChannelWriter<Piece> _channelWriter;
    private int _currentChokedCount;
    private int _currentEmptyBitfieldCount;
    private bool _choked = true;
    private Bitfield _bitfield;

    public Client(
      Peer peer,
      ConcurrentQueue<RequestItem> items,
      ChannelWriter<Piece> channelWriter,
      byte[] infoHash
    )
    {
      _items = items;
      _channelWriter = channelWriter;
      _messageHandler = new MessageHandler();
      _bittorrent = new BittorrentProtocol(peer);
      _bittorrent.EstablishConnection();
      _bittorrent.PeerHandshake(Handshake.Create(infoHash, PeerId.CreateNew()));
      _messageHandler.OnBitfieldReceived += @event => { _bitfield = @event.Bitfield; };
      _messageHandler.OnHaveReceived += @event => { _bitfield.SetPiece(@event.Index); };
      _messageHandler.OnChokeReceived += () => { _choked = true; };
      _messageHandler.OnUnchokeReceived += () => { _choked = false; };
    }

    public async Task Process()
    {
      RequestItem item = null;
      try
      {
        _messageHandler.Handle(_bittorrent.ReadMessage());

        while (!_items.IsEmpty)
        {
          if (!_items.TryDequeue(out item)) continue;
          if (_bitfield == null || !_bitfield.HasPiece(item.Index))
          {
            _items.Enqueue(item);
            _currentEmptyBitfieldCount++;

            if (_currentEmptyBitfieldCount == MAX_EMPTY_BITFIELD_COUNT) return;

            continue;
          }

          var piece = new Piece {Index = item.Index, Buffer = new byte[item.Length]};

          _messageHandler.OnPieceReceived += PieceCallback;

          Console.WriteLine($"Downloading piece {item.Index}");

          while (piece.Downloaded < item.Length)
          {
            if (_choked)
            {
              _bittorrent.SendMessage(new UnchokeMessage());
              _bittorrent.SendMessage(new InterestedMessage());

              _messageHandler.Handle(_bittorrent.ReadMessage());

              _currentChokedCount++;

              if (_currentChokedCount == MAX_CHOKED_COUNT)
              {
                _items.Enqueue(item);
                return;
              }

              continue;
            }

            while (piece.Requested < item.Length)
            {
              if (item.Length - piece.Requested < piece.BlockSize)
              {
                piece.BlockSize = item.Length - piece.Requested;
              }

              _bittorrent.SendMessage(new RequestMessage(new PieceBlock(item.Index, piece.Requested, piece.BlockSize)));
              piece.Requested += piece.BlockSize;
            }

            _messageHandler.Handle(_bittorrent.ReadMessage());
          }

          _messageHandler.OnPieceReceived -= PieceCallback;

          if (!piece.CheckIntegrity(item.Hash))
          {
            Console.WriteLine("Failed integrity check");
            _items.Enqueue(item);
            continue;
          }

          if (piece.Downloaded == item.Length)
          {
            Console.WriteLine($"Downloaded piece {item.Index}");
            await _channelWriter.WriteAsync(piece);
          }

          void PieceCallback(PieceEventArgs e)
          {
            piece.Download(e.Payload);
          }
        }
      }
      catch (PeerCommunicationException e)
      {
        if (item != null) _items.Enqueue(item);
        Console.WriteLine(e.Message);
      }
    }
  }
}