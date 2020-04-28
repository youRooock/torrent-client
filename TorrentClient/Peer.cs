using System;
using System.Net;
using TorrentClient.Exceptions;

namespace TorrentClient
{
  public class Peer : IDisposable
  {
    private Connection Connection { get; set; }

    // ReSharper disable once InconsistentNaming
    public IPEndPoint IPEndPoint { get; }

    private Peer(IPEndPoint ipEndPoint)
    {
      IPEndPoint = ipEndPoint;
    }

    public static Peer Create(IPEndPoint ipEndPoint) => new Peer(ipEndPoint);

    public void Connect()
    {
      try
      {
        Connection = new Connection(IPEndPoint);
      }

      catch (Exception)
      {
        throw new PeerCommunicationException($"[{IPEndPoint}] Failed to connect peer");
      }
    }

    public byte[] ReadData(int length)
    {
      return ReadBytesInternal(length);
    }

    public byte[] ReadBytes()
    {
      int length;
      try
      {
        int byteSize = Connection.Reader.ReadInt32();
        if (byteSize == 0) return null;
        length = IPAddress.NetworkToHostOrder(byteSize);
      }
      catch (Exception)
      {
        throw new PeerCommunicationException($"[{IPEndPoint}] Failed to send bytes to peer");
      }

      if (length == 0) return null;

      return ReadBytesInternal(length);
    }

    public void Dispose()
    {
      Connection?.Dispose();
    }

    public void SendBytes(byte[] data)
    {
      try
      {
        Connection.Writer.Write(data);
        Connection.Writer.Flush();
      }
      catch (Exception)
      {
        throw new PeerCommunicationException($"[{IPEndPoint}] Failed to send bytes to peer");
      }
    }

    private byte[] ReadBytesInternal(int length)
    {
      try
      {
        return Connection.Reader.ReadBytes(length);
      }
      catch (Exception)
      {
        throw new PeerCommunicationException($"[{IPEndPoint}] Failed to read bytes from peer");
      }
    }
  }
}