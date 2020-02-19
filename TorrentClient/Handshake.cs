using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TorrentClient
{
  public class Handshake
  {
    private const int MaxSize = 68;
    private readonly byte[] _emptyArr = new byte[8];
    private readonly List<byte> _handshakeBytes;

    private Handshake(byte[] protocolName, byte[] infoHash, byte[] peerId)
    {
      _handshakeBytes = new List<byte>(infoHash.Length + peerId.Length + protocolName.Length + _emptyArr.Length + 1);

      _handshakeBytes.Add((byte)protocolName.Length);
      _handshakeBytes.AddRange(protocolName);
      _handshakeBytes.AddRange(_emptyArr);
      _handshakeBytes.AddRange(infoHash);
      _handshakeBytes.AddRange(peerId);
    }

    public static Handshake Create(byte[] infoHash, byte[] peerId)
    {
      return new Handshake(Encoding.UTF8.GetBytes("BitTorrent protocol"), infoHash, peerId);
    }

    public static Handshake Parse(Span<byte> array)
    {
      if (array.Length < MaxSize) return null;
      return new Handshake(
        array.Slice(1, 19).ToArray(),
        array.Slice(28, 20).ToArray(),
        array.Slice(48, 20).ToArray());
    }

    public byte[] Bytes => _handshakeBytes.ToArray();

    public override int GetHashCode()
    {
      return 17 * Bytes.Length ^ base.GetHashCode();
    }

    public override bool Equals(object o)
    {
      if (o is Handshake handshake)
      {
        return handshake.ExtractInfoHash().SequenceEqual(ExtractInfoHash())
          && handshake.ExtractProtocolName().SequenceEqual(ExtractProtocolName());
      }

      return false;
    }

    private Span<byte> ExtractInfoHash()
    {
      return _handshakeBytes
        .ToArray()
        .AsSpan()
        .Slice(28, 20);
    }

    private Span<byte> ExtractProtocolName()
    {
      return _handshakeBytes
        .ToArray()
        .AsSpan()
        .Slice(1, 19);
    }
  }
}
