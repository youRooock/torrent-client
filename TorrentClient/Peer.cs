using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TorrentClient.Messages;
using TorrentClient;

namespace TorrentClient
{
  public class Peer
  {
    public Connection Connection { get; private set; }
    public bool IsChoked { get; set; } = true;

    public int Downloaded { get; set; } = 0;

    public Peer(IPEndPoint ipEndPoint)
    {
      IPEndPoint = ipEndPoint;
    }

    public static Peer Create(IPEndPoint ipEndPoint) => new Peer(ipEndPoint);

    public IPEndPoint IPEndPoint { get; }
    public bool IsConnected { get; private set; }

    public bool TryConnect()
    {
      try
      {
        if (!IsConnected)
        {
          Connection = new Connection(IPEndPoint);
          IsConnected = true;
        }

        return true;
      }

      catch (SocketException)
      {
        return false;
      }
    }

    public bool TryHandshake(byte[] infoHash, byte[] peerId)
    {
      var handshake = Handshake.Create(infoHash, peerId);
      if(!TrySend(handshake.Bytes)) return false;
      var hs = new byte[68];
      if(!TryReadData(hs)) return false;

      var handshake2 = Handshake.Parse(hs);

      if (!handshake.Equals(handshake2))
      {
        Console.WriteLine($"[{IPEndPoint}] Handshake failed");
        return false;
      }

      return true;
    }

    public void SendRequestMessage(BlockRequest request)
    {
      var message = new RequestMessage(request);
      Send(message.Serialize());
    }

    public void SendUnchokeMessage()
    {
      var bytes = new UnchokeMessage().Serialize();

      Send(bytes);
    }

    public void SendInterestedMessage()
    {
      var bytes = new InterestedMessage().Serialize();

      Send(bytes);
    }

    public void SendHaveMessage(long index)
    {
      var message = new HaveMessage(index);
      Send(message.Serialize());
    }

    public void Send(byte[] data)
    {
      Connection.Write(data);
    }

    public bool TrySend(byte[] data)
    {
      try
      {
        Connection.Write(data);
        return true;
      }
      catch (Exception e)
      {
        Console.WriteLine($"[{IPEndPoint}] failed to write");
        return false;
      }
    }

    public bool TryReadData(byte[] arr)
    {
      try
      {
        Connection.Read(arr);
        return true;
      }
      catch (Exception)
      {
        Console.WriteLine($"[{IPEndPoint}] failed to read");
        return false;
      }
    }

    public Message ReadMessage()
    {
      // int length = Connection.BinarReader.ReadInt32();

      int value = Connection.Read();
      var length = IPAddress.NetworkToHostOrder(value);
      if (length == 0) return null;

      byte[] data = Connection.Read(length);

      var msg = new Message(data);

      return msg;
    }


    public event Action<PieceEvent> OnPieceReceived;
    
    public void ReadData()
    {
      Task.Factory.StartNew(() =>
      {
        try
        {
          while (true)
          {
            // Read message length
            int length = Connection.Read();

            if (length == 0)
            {
              Console.WriteLine($"[{IPEndPoint}] keep alive");
              continue;
            }

            // Read data
            byte[] data = Connection.Read(length);

            var m = new Message(data);

            if (m.Id == MessageId.Unchoke)
            {
              IsChoked = false;
            }

            if (m.Id == MessageId.Piece)
            {
              OnPieceReceived?.Invoke(new PieceEvent());
            }
          }
        }
        catch (IOException)
        {
          IsConnected = false;
          Console.WriteLine($"[{IPEndPoint}] Disconnected");
        }
      }, TaskCreationOptions.LongRunning);
    }
  }

  public class PieceEvent: EventArgs
  {
    public PieceEvent()
    {
      
    }
  }
}