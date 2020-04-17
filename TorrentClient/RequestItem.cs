namespace TorrentClient
{
  public class RequestItem
  {
    public RequestItem(int index, byte[] hash, long length)
    {
      Index = index;
      Hash = hash;
      Length = length;
    }

    public int Index { get; }
    public byte[] Hash { get; }
    public long Length { get; }
  }
}