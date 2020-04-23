using System;
using System.IO;
using System.Net;
using TorrentClient.Exceptions;
using TorrentClient.Messages;

namespace TorrentClient
{
  public class Peer : IDisposable
  {
    private Connection Connection { get; set; }
    private bool IsConnected { get; set; }
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
        if (!IsConnected)
        {
          Connection = new Connection(IPEndPoint);
          IsConnected = true;
        }
      }

      catch (Exception)
      {
        IsConnected = false;
      }
    }

    public byte[] ReadData(int length)
    {
      return Connection.Reader.ReadBytes(length);
    }

    public byte[] ReadBytes()
    {
      int byteSize = Connection.Reader.ReadInt32();
      if (byteSize == 0) return null;
      var length = IPAddress.NetworkToHostOrder(byteSize);
      if (length == 0) return null;

      byte[] data = Connection.Reader.ReadBytes(length);

      return data;
    }

    public void Dispose()
    {
      Connection?.Dispose();
    }

    public void SendBytes(byte[] data)
    {
      try
      {
        if (IsConnected)
        {
          Connection.Writer.Write(data);
          Connection.Writer.Flush();
        }
        else
        {
          throw new PeerCommunicationException($"[{IPEndPoint}] disconnected from peer");
        }
      }
      catch (IOException)
      {
        IsConnected = false;
      }
    }
  }
}