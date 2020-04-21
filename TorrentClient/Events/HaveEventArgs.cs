using System;

namespace TorrentClient.Events
{
  public class HaveEventArgs : EventArgs
  {
    public int Index { get; set; }
  }
}