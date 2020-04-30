using System;
using System.IO;
using BencodeNET.Parsing;
using BencodeNET.Torrents;

namespace TorrentClient
{
  public class TorrentFileInfo
  {
    private readonly Torrent _torrent;

    public TorrentFileInfo(string path)
    {
      if (!File.Exists(path))
        throw new ArgumentException($"{path} doesn't exist");
      if (Path.GetExtension(path) != ".torrent")
        throw new ArgumentException("provided file should have .torrent extension");
      BencodeParser parser = new BencodeParser();
      _torrent = parser.Parse<Torrent>(path);
      PieceHashes = SplitPieceHashes();
    }

    public long Size => _torrent.File.FileSize;
    public string Announce => _torrent.Trackers[0][0];
    public byte[] InfoHash => _torrent.GetInfoHashBytes();
    public long PieceSize => _torrent.PieceSize;
    public byte[][] PieceHashes { get; }
    public string Name => _torrent.DisplayName;

    private byte[][] SplitPieceHashes()
    {
      var hashLength = 20;
      var hashNums =  _torrent.Pieces.Length / hashLength;
      byte[][] pieceHashes = new byte[hashNums][];

      for (int i = 0, offset = 0; i < hashNums; i++, offset += hashLength)
      {
        pieceHashes[i] = _torrent.Pieces[offset..(offset + hashLength)];
      }

      return pieceHashes;
    }
  }
}