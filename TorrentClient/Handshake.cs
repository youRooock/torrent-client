using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TorrentClient
{
  public class Handshake
  {
    private const string ProtocolName = "BitTorrent protocol";
    private readonly byte[] _emptyArr = new byte[8];
    private readonly List<byte> _handshakeBytes;


    public Handshake(byte[] infoHash, byte[] peerId)
    {
      _handshakeBytes = new List<byte>(infoHash.Length + peerId.Length + ProtocolName.Length + _emptyArr.Length + 1);

      _handshakeBytes.Add((byte)ProtocolName.Length);
      _handshakeBytes.AddRange(Encoding.UTF8.GetBytes(ProtocolName));
      _handshakeBytes.AddRange(_emptyArr);
      _handshakeBytes.AddRange(infoHash);
      _handshakeBytes.AddRange(peerId);
    }

    public byte[] Bytes => _handshakeBytes.ToArray();
  }
}
