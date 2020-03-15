namespace TorrentClient
{
  public class PieceProgress
  {
    public int Index { get; set; }
    public byte[] Buffer { get; set; }
    public long Downloaded { get; set; }
    public long Requested { get; set; }
    public long Backlog { get; set; }
  }
}
