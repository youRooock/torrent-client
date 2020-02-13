using System;

namespace TorrentClient
{
  class Program
  {
    static void Main(string[] args)
    {
      var torrentFileInfo = new TorrentFileInfo("ubuntu-19.10-desktop-amd64.iso.torrent");

      var tracker = new TorrentTracker(torrentFileInfo.Announce);

      var peer = tracker.RetrievePeers(torrentFileInfo.InfoHash, torrentFileInfo.Size).Result;
    }
  }
}
