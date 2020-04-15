using System;

namespace TorrentClient.Exceptions
{
  public class PeerCommunicationException : Exception
  {
    public PeerCommunicationException(string message) : base(message)
    {
    }
  }
}