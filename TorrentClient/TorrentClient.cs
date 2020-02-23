using System;
using System.Net.Sockets;

namespace TorrentClient
{
  public class TorrentClient : IDisposable
  {
    private readonly NetworkStream _ns;
    private readonly byte[] _bitfield;

    public TorrentClient(NetworkStream ns, byte[] bitfield)
    {
      _ns = ns;
      _bitfield = bitfield;
    }

    public void Dispose()
    {
      _ns.Dispose();
    }
  }
}
