using System;

namespace TorrentClient
{
  public static class PeerId
  {
    public static byte[] CreateNew()
    {
      var peerId = new byte[20];
      new Random().NextBytes(peerId);

      return peerId;
    }
  }
}