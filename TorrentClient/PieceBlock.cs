namespace TorrentClient
{
  public class PieceBlock
  {
    public long Index { get; }
    public long Offset { get; }
    public long Length { get; }

    public PieceBlock(long index, long offset, long length)
    {
      Index = index;
      Offset = offset;
      Length = length;
    }
  }
}