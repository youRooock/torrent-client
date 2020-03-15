using System;
using BencodeNET.Parsing;
using BencodeNET.Torrents;

namespace TorrentClient
{
  public class TorrentFileInfo
  {
    private readonly Torrent _torrent;

    public TorrentFileInfo(string path)
    {
      // ToDo: provide some validations for path
      BencodeParser parser = new BencodeParser();
      _torrent = parser.Parse<Torrent>(path);
      PieceHashes = SplitPieceHashes();
    }

    public long Size => _torrent.File.FileSize;
    public string Announce => _torrent.Trackers[0][0];
    public byte[] InfoHash => _torrent.GetInfoHashBytes();
    public long PieceSize => _torrent.PieceSize;
    public byte[] Pieces => _torrent.Pieces;
    public byte[][] PieceHashes { get; }

    private byte[][] SplitPieceHashes()
    {
      var hashLength = 20;
      var piecesSpan = Pieces.AsSpan();
      var hashNums = Pieces.Length / hashLength;
      byte[][] pieceHases = new byte[hashNums][]; 

      for (int i = 0, offset = 0; i < hashNums; i++, offset += hashLength)
      {
        pieceHases[i] = piecesSpan.Slice(offset, hashLength).ToArray();
      }

      return pieceHases;
    }
  }
}
