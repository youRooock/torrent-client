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
    public Bitfield Bitfield { get; private set; }
    public bool IsChoked { get; set; } = true;

    public int Downloaded { get; set; } = 0;

    public Peer(IPEndPoint ipEndPoint)
    {
      IPEndPoint = ipEndPoint;
    }

    public static Peer Create(IPEndPoint ipEndPoint) => new Peer(ipEndPoint);

    public IPEndPoint IPEndPoint { get; }
    public bool IsConnected { get; private set; }

    public bool TryConnect(byte[] infoHash, byte[] peerId)
    {
      try
      {
        if (!IsConnected)
        {
          Connection = new Connection(IPEndPoint);
          HandshakePeer(infoHash, peerId);
          Bitfield = RetrieveBitfield();

          IsConnected = true;
        }

        return true;
      }

      catch (SocketException)
      {
        return false;
      }
    }


    public Bitfield RetrieveBitfield()
    {
      var mgs = ReadMessage();
      
      return new Bitfield(mgs.Payload);
    }

    public void HandshakePeer(byte[] infoHash, byte[] peerId)
    {
      var handshake = Handshake.Create(infoHash, peerId);

      Send(handshake.Bytes);

      var hs = ReadData(68);
      var handshake2 = Handshake.Parse(hs);

      if (!handshake.Equals(handshake2))
      {
        Console.WriteLine($"[{IPEndPoint}] Handshake failed");

        throw new Exception($"[{IPEndPoint}] Handshake failed");
      }
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

    public byte[] ReadData(int length)
    {
      return Connection.Read(length);
     }

    public Message ReadMessage()
    {
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