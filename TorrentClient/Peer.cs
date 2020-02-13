using System;
using System.Net;

namespace TorrentClient
{
  public class Peer
  {
    public Peer(IPAddress ip, int port)
    {
      IPEndPoint = new IPEndPoint(ip, port);
    }

    public IPEndPoint IPEndPoint { get; }
  }
}
