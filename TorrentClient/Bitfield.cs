namespace TorrentClient
{
  public class Bitfield
  {
    private readonly byte[] _arr;

    public Bitfield(byte[] arr)
    {
      _arr = arr;
    }

    public bool HasPiece(int index)
    {
      var byteIndex = index / 8;
      var offset = index % 8;
      if (byteIndex < 0 || byteIndex >= _arr.Length)
      {
        return false;

      }
      return (_arr[byteIndex] >> (7 - offset) & 1) != 0;
    }

    public void SetPiece(int index)
    {
      var byteIndex = index / 8;
      var offset = index % 8;
      if (byteIndex < 0 || byteIndex >= _arr.Length)
      {
        return;

      }
      _arr[byteIndex] |= (byte)(1 << (7 - offset));
    }
  }
}
