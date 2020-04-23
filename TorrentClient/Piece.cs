using System;
using System.Linq;
using System.Security.Cryptography;

namespace TorrentClient
{
  public class Piece
  {
    public int Index { get; set; }
    public byte[] Buffer { get; set; }
    public long Downloaded { get; set; }
    public long Requested { get; set; }
    public long BlockSize { get; set; } = 16384;

    public bool CheckIntegrity(byte[] hash)
    {
      SHA1 sha = new SHA1CryptoServiceProvider();
      var computedHash = sha.ComputeHash(Buffer);
      return hash.SequenceEqual(computedHash);
    }
  }
}