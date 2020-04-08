namespace TorrentClient
{
  public class BlockRequest
  {
    public long Index { get; }
    public long Offset { get; }
    public long Length { get; }

    public BlockRequest(long index, long offset, long length)
    {
      Index = index;
      Offset = offset;
      Length = length;
    }
  }
}