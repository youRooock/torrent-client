﻿using System;
using System.Linq;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class Message
  {
    private const int MessageSizeLength = 4; // 4 bytes for determining length

    public byte[] Payload { get; set; }
    public MessageId Id { get; set; }

    public Message()
    {

    }

    public Message(byte[] arr)
    {
      Id = (MessageId)arr[0];
      Payload = arr[1..];
    }

    public byte[] Serialize()
    {
      var buffer = new byte[MessageSizeLength + TotalMessageSize];
      BigEndian.PutUint32(buffer, TotalMessageSize);
      buffer[4] = (byte)Id;

      Array.Copy(Payload ?? new byte[0], 0, buffer, 5, Payload?.Length ?? 0);

      return buffer.ToArray();
    }

    public int ParsePiece(int index, byte[] buffer)
    {
      if (Id != MessageId.Piece) return 0;
      if (Payload.Length < 0) return 0;
      var parsedIndex = BigEndian.ToUint32(Payload[..4]);

      if (index != parsedIndex) return 0;

      var begin = BigEndian.ToUint32(Payload[4..8]);

      if (begin > buffer.Length) return 0;

      var data = Payload[8..];

      if (begin + data.Length > buffer.Length) return 0;

      Array.Copy(data, 0, buffer, begin, data.Length);

      return data.Length;
    }

    private int TotalMessageSize => (Payload?.Length ?? 0) + 1; // extra byte for id
  }

  public enum MessageId
  {
    Choke = 0,
    Unchoke,
    Interested,
    NotInterested,
    Have,
    Bitfield,
    Request,
    Piece,
    Cancel
  }
}
