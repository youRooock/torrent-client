using System;

namespace TorrentClient.Events
{
  public class PieceEventArgs: EventArgs
  {
    public byte[] Payload { get; set; }
  }
}