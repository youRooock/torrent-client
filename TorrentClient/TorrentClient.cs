﻿using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class TorrentClient : IDisposable
  {
    private readonly NetworkStream _ns;
    private readonly TcpClient _cl;

    public TorrentClient(TcpClient cl)
    {
      _cl = cl;
      _ns = cl.GetStream();
    }

    public async Task<byte[]> GetBitmapField()
    {
      byte[] messageLength = new byte[4];
      await _ns.ReadAsync(messageLength, 0, messageLength.Length);

      var length = BigEndian.ToUint32(messageLength);

      if (length == 0) return null;

      var messageBytes = new byte[length];

      await ReadWholeArray(messageBytes);


      // var read = await _ns.ReadAsync(messageBytes, 0, messageBytes.Length);

      var message = new Message(messageBytes);

      if (message.Id != MessageId.Bitfield)
      {
        Console.WriteLine("client didn't respond with bitfield id");
        return null;
      }

      Console.WriteLine($"{message.Id} {string.Join(" ", message.Payload)}");

      return message.Payload;
    }

    public async Task SendUnchoke()
    {
      var message = new Message { Id = MessageId.Unchoke }.Serialize();

      await _ns.WriteAsync(message, 0, message.Length);
    }

    public async Task SendInterested()
    {
      var message = new Message { Id = MessageId.Interested }.Serialize();

      await _ns.WriteAsync(message, 0, message.Length);
    }

    public void Dispose()
    {
      _cl.Dispose();
      _ns.Dispose();
    }

    private async Task ReadWholeArray(byte[] data)
    {
      var offset = 0;
      var remaining = data.Length;
      while (remaining > 0)
      {
        var read = await _ns.ReadAsync(data, offset, remaining);
        remaining -= read;
        offset += read;
      }
    }
  }
}
