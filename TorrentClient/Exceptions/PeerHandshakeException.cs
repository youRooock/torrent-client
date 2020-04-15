using System;

namespace TorrentClient.Exceptions
{
  public class PeerHandshakeException : Exception
  {
    public PeerHandshakeException(string message) : base(message)
    {
    }
  }
}