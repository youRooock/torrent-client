using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient
{
  public class Handshake
  {
    private const int Size = 68;
    private readonly byte[] _emptyArr = new byte[8];
    private readonly List<byte> _handshakeBytes;
    private readonly string _protocolName;
    private readonly byte[] _infoHash;
    private readonly byte[] _peerId;


    private Handshake(string protocolName, byte[] infoHash, byte[] peerId)
    {
      _protocolName = protocolName;
      _infoHash = infoHash;
      _peerId = peerId;
      _handshakeBytes = new List<byte>(_infoHash.Length + _peerId.Length + _protocolName.Length + _emptyArr.Length + 1);

      _handshakeBytes.Add((byte)_protocolName.Length);
      _handshakeBytes.AddRange(Encoding.UTF8.GetBytes(_protocolName));
      _handshakeBytes.AddRange(_emptyArr);
      _handshakeBytes.AddRange(_infoHash);
      _handshakeBytes.AddRange(_peerId);
    }

    public static Handshake Create(byte[] infoHash, byte[] peerId)
    {
      return new Handshake("BitTorrent protocol", infoHash, peerId);
    }

    public static Handshake Parse(Span<byte> array)
    {
      if (array.Length < 68) return null;
      return new Handshake(
        Encoding.UTF8.GetString(array.Slice(1, 19).ToArray()),
        array.Slice(28, 20).ToArray(),
        array.Slice(48, 20).ToArray());
    }

    public byte[] Bytes => _handshakeBytes.ToArray();

    public override int GetHashCode()
    {
      return 17 * Bytes.Length ^ _protocolName.GetHashCode();
    }

    public override bool Equals(object o)
    {
      if (o is Handshake handshake)
      {
        return handshake._infoHash == _infoHash && handshake._protocolName == _protocolName;
      }

      return false;
    }
  }
}
