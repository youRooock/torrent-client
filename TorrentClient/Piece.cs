using System;
using System.Linq;
using System.Security.Cryptography;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class Piece
  {
    public int Index { get; set; }
    public byte[] Buffer { get; set; }
    public long Downloaded { get; private set; }
    public long Requested { get; set; }
    public long BlockSize { get; set; } = 16384;

    public bool CheckIntegrity(byte[] hash)
    {
      SHA1 sha = new SHA1CryptoServiceProvider();
      var computedHash = sha.ComputeHash(Buffer);
      return hash.SequenceEqual(computedHash);
    }

    public void Download(byte[] payload)
    {
      var begin = BigEndian.ToUint32(payload[4..8]);
      var data = payload[8..];

      Array.Copy(data, 0, Buffer, begin, data.Length);
      Downloaded += data.Length;
    }
  }
}