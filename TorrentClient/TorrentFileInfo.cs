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
    }

    public long Size => _torrent.File.FileSize;
    public string Announce => _torrent.Trackers[0][0];
    public byte[] InfoHash => _torrent.GetInfoHashBytes();
  }
}
