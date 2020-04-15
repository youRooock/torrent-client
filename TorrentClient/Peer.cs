using System;
using System.IO;
using System.Net;
using TorrentClient.Messages;

namespace TorrentClient
{
  public class Peer : IDisposable
  {
    private Connection Connection { get; set; }
    private bool IsConnected { get; set; }
    public Bitfield Bitfield { get; private set; }

    // ReSharper disable once InconsistentNaming
    public IPEndPoint IPEndPoint { get; }
    public bool IsChoked { get; set; } = true;

    private Peer(IPEndPoint ipEndPoint)
    {
      IPEndPoint = ipEndPoint;
    }

    public static Peer Create(IPEndPoint ipEndPoint) => new Peer(ipEndPoint);

    public bool TryConnect(byte[] infoHash)
    {
      try
      {
        if (!IsConnected)
        {
          Connection = new Connection(IPEndPoint);
          IsConnected = true;
          HandshakePeer(Handshake.Create(infoHash, PeerId.CreateNew()));
          Bitfield = RetrieveBitfield();
        }

        return true;
      }

      catch (Exception)
      {
        IsConnected = false;
        return false;
      }
    }

    private Bitfield RetrieveBitfield()
    {
      var mgs = ReadMessage();

      return new Bitfield(mgs.Payload);
    }

    private void HandshakePeer(Handshake handshake)
    {
      SendInternal(handshake.Bytes);

      var hs = ReadData(68);
      var handshake2 = Handshake.Parse(hs);

      if (!handshake.Equals(handshake2))
      {
        Console.WriteLine($"[{IPEndPoint}] Handshake failed");

        throw new Exception($"[{IPEndPoint}] Handshake failed");
      }
    }

    public void SendMessage(IMessage message) => SendInternal(message.Serialize());


    public IMessage ReadMessage()
    {
      var bytes = ReadInternal();
      
      
    }

    public byte[] ReadData(int length)
    {
      return Connection.Read(length);
    }

    public Message ReadMessage()
    {
      int value = Connection.ReadSize();
      var length = IPAddress.NetworkToHostOrder(value);
      if (length == 0) return null;

      byte[] data = Connection.Read(length);

      var msg = new Message(data);

      return msg;
    }

    public void Dispose()
    {
      Connection?.Dispose();
    }

    public byte[] ReadInternal()
    {
      try
      {
        int byteSize = Connection.ReadSize();
        if (byteSize == 0) return null;

        return Connection.Read(byteSize);
      }

      catch (IOException)
      {
        IsConnected = false;
        return null;
      }
    }

    public void SendInternal(byte[] data)
    {
      try
      {
        if (IsConnected)
        {
          Connection.Write(data);
        }
        else
        {
          throw new Exception();
        }
      }
      catch (IOException)
      {
        IsConnected = false;
      }
    }
  }
}