namespace TorrentClient
{
  public class RequestItem
  {
    public RequestItem(int index, byte[] hash, long pieceSize, long totalSize)
    {
      Index = index;
      Hash = hash;
      Length = CalculateItemLength(index, pieceSize, totalSize);
    }

    public int Index { get; }
    public byte[] Hash { get; }

    public long Length { get; }

    private long CalculateItemLength(int index, long pieceSize, long totalSize)
    {
      var begin = index * pieceSize;
      var end = begin + pieceSize;

      if (end > totalSize)
      {
        end = totalSize;
      }

      return end - begin;
    }
  }
}