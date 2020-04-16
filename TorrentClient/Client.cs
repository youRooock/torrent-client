using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TorrentClient.Events;
using TorrentClient.Messages;

namespace TorrentClient
{
  public class Client
  {
    private readonly BittorrentProtocol _bittorrent;
    private readonly MessageHandler _messageHandler;
    private readonly ConcurrentQueue<RequestItem> _items;
    private readonly ChannelWriter<Piece> _channelWriter;
    private bool _choked = true;
    private long _downloadedBytes;
    private Bitfield _bitfield;

    public Client(Peer peer, ConcurrentQueue<RequestItem> items, ChannelWriter<Piece> channelWriter)
    {
      _items = items;
      _channelWriter = channelWriter;
      _messageHandler = new MessageHandler();
      _bittorrent = new BittorrentProtocol(peer, _messageHandler);
      _bittorrent.EstablishConnection();
      _messageHandler.OnBitfieldReceived += @event => { _bitfield = @event.Bitfield; };
      _messageHandler.OnChokeReceived += () => { _choked = true; };
      _messageHandler.OnUnchokeReceived += () => { _choked = false; };
      _messageHandler.OnPieceReceived += 
    }

    private void HandlePiece(PieceEventArgs ev)
    {
      
    }

    public async Task Process()
    {
      var cts = new CancellationTokenSource();

      _bittorrent.SendMessage(new UnchokeMessage());
      _bittorrent.SendMessage(new InterestedMessage());

      var readMessagesTask = _bittorrent.ReadMessagesAsync(cts.Token);

      while (!_items.IsEmpty)
      {
        _downloadedBytes = 0;
        var piece = new Piece();

        if (!_items.TryDequeue(out var item)) continue;
        if (!_bitfield.HasPiece(item.Index))
        {
          _items.Enqueue(item);
          continue;
        }

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
          
          var n = ParsePiece(piece.Index, piece.Buffer);

          piece.Downloaded += n;
        }
      }
    }
  }