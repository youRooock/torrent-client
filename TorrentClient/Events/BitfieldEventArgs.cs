using System;

namespace TorrentClient.Events
{
  public class BitfieldEventArgs: EventArgs
  {
    public Bitfield Bitfield { get; set; }
  }
}