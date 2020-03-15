using System;
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

    public async Task<Bitfield> GetBitmapField()
    {
      byte[] messageLength = new byte[4];

      //await ReadWholeArray(messageLength);
      _connection.Read(messageLength);

      var length = BigEndian.ToUint32(messageLength);

      if (length == 0) return null;

      var messageBytes = new byte[length];

      //await ReadWholeArray(messageBytes);
      _connection.Read(messageBytes);

      var message = new Message(messageBytes);

      if (message.Id != MessageId.Bitfield)
      {
        Console.WriteLine("client didn't respond with bitfield id");
        return null;
      }

      Console.WriteLine($"{message.Id} {string.Join(" ", message.Payload)}");

      return new Bitfield(message.Payload);
    }

    public byte[] TryDownload(int index, long length)
    {
      var state = new PieceProgress
      {
        Index = index,
        Buffer = new byte[length]
      };


      while (state.Downloaded < length)
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

          //_ns.WriteAsync(message, 0, message.Length).Wait();

          state.Backlog++;

          state.Requested += blockSize;
        }

        byte[] messageLength = new byte[4];

        //ReadWholeArray(messageLength).Wait();
        _connection.Read(messageLength);

        var l = BigEndian.ToUint32(messageLength);

        if (l == 0) return null;

        var messageBytes = new byte[length];

        //ReadWholeArray(messageBytes).Wait();
        _connection.Read(messageBytes);

        var m = new Message(messageBytes);
      }

      return state.Buffer;
    }

    public async Task SendUnchoke()
    {
      var message = new Message { Id = MessageId.Unchoke }.Serialize();

      //await _ns.WriteAsync(message, 0, message.Length);
      _connection.Write(message);
    }

    public async Task SendInterested()
    {
      var message = new Message { Id = MessageId.Interested }.Serialize();

      //await _ns.WriteAsync(message, 0, message.Length);
      _connection.Write(message);
    }

    public void Dispose()
    {
      _connection.Dispose();
      //_cl.Dispose();
      //_ns.Dispose();
    }

    //private async Task ReadWholeArray(byte[] data)
    //{
    //  var offset = 0;
    //  var remaining = data.Length;
    //  while (remaining > 0)
    //  {
    //    var read = await _ns.ReadAsync(data, offset, remaining);
    //    remaining -= read;
    //    offset += read;
    //  }
    //}
  }
}