using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class TorrentClient : IDisposable
  {
    private readonly Connection _connection;

    public TorrentClient(Connection c)
    {
      _connection = c;
    }

    public Bitfield GetBitmapField()
    {
      byte[] messageLength = new byte[4];
      _connection.Read(messageLength);

      var length = BigEndian.ToUint32(messageLength);

      if (length == 0) return null;

      var messageBytes = new byte[length];
      _connection.Read(messageBytes);

      var message = new Message(messageBytes);

      if (message.Id != MessageId.Bitfield)
      {
        Console.WriteLine("client didn't respond with bitfield id");
        return null;
      }
      
      return new Bitfield(message.Payload);
    }

    public byte[] TryDownload(int index, long length)
    {
      var state = new PieceProgress
      {
        Index = index,
        Buffer = new byte[length]
      };
      bool choked = true;
      while (state.Downloaded < length)
      {
        if (!choked)
        {
          while (state.Backlog < 5 && state.Requested < length)
          {
            long blockSize = 16384;

            if (length - state.Requested < blockSize)
            {
              blockSize = length - state.Requested;
            }

            Span<byte> payload = stackalloc byte[12];

            BigEndian.PutUint32(payload.Slice(0, 4), index);
            BigEndian.PutUint32(payload.Slice(4, 4), state.Requested);
            BigEndian.PutUint32(payload.Slice(8, 4), blockSize);

            var message = new Message
            {
              Id = MessageId.Request,
              Payload = payload.ToArray()
            }.Serialize();

            _connection.Write(message);

            state.Backlog++;
            state.Requested += blockSize;
          }
        }

        byte[] messageLength = new byte[4];
        _connection.Read(messageLength);

        var l = BigEndian.ToUint32(messageLength);

        if (l <= 0)
        {
          
        }

        var messageBytes = new byte[l];
        _connection.Read(messageBytes);
        var m = new Message(messageBytes);

        switch (m.Id)
        {
          case MessageId.Piece:
            var n = m.ParsePiece(state.Index, state.Buffer);
            state.Downloaded += n;
            state.Backlog--;
            break;
          case MessageId.Choke:
            throw new Exception("choked");
          case MessageId.Unchoke:
            Console.WriteLine("Unchoke");
            choked = false;
            break;
          case MessageId.Interested:
            break;
          case MessageId.NotInterested:
            break;
          case MessageId.Have:
            Console.WriteLine("have");
            break; //ToDo: handle
          case MessageId.Bitfield:
            break;
          case MessageId.Request:
            break;
          case MessageId.Cancel:
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }

      return state.Buffer;
    }

    // private void Loop()
    // {
    //   _connection.
    // }

    public void SendUnchoke()
    {
      var message = new Message {Id = MessageId.Unchoke}.Serialize();
      _connection.Write(message);
    }

    public void SendInterested()
    {
      var message = new Message {Id = MessageId.Interested}.Serialize();
      _connection.Write(message);
    }
    
    public void SendHave(long index)
    {
      var paylod = new byte[4];
      BigEndian.PutUint32(paylod, index);
      var message = new Message {Id = MessageId.Have, Payload = paylod}.Serialize();
      _connection.Write(message);
    }

    public void Dispose()
    {
      _connection.Dispose();
    }
  }
}