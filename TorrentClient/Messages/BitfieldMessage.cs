using System;

namespace TorrentClient.Messages
{
  public class BitfieldMessage : IResponseMessage
  {
    public BitfieldMessage(byte[] arr)
    {
      arr = arr ?? throw new ArgumentNullException(nameof(arr));
      if (arr[0] != Id) throw new ArgumentException("Byte array should be of bitfield message");

      Payload = arr[1..];
    }

    public byte[] Payload { get; }
    public byte Id => 5;
  }
}