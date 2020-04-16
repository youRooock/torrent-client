using System;

namespace TorrentClient.Events
{
  public class PieceEventArgs: EventArgs
  {
    public Piece Piece { get; set; }
  }
}