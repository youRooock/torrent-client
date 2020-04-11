using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TorrentClient.Messages;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class Peer
  {
    public Channel<byte[]> Data = Channel.CreateUnbounded<byte[]>();
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

      catch (Exception)
      {
        return false;
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
      catch (Exception)
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


    // public void ReadData()
    // {
    //   Task.Factory.StartNew(() =>
    //   {
    //     try
    //     {
    //       while (true)
    //       {
    //         // Read message length
    //         int length = Connection.BinarReader.ReadInt32();
    //
    //         if (length == 0)
    //         {
    //           Console.WriteLine($"[{IPEndPoint}] keep alive");
    //           continue;
    //         }
    //
    //         // Read data
    //         byte[] data = Connection.BinarReader.ReadBytes(length);
    //
    //         var m = new Message(data);
    //
    //         switch (m.Id)
    //         {
    //           case MessageId.Piece:
    //             break;
    //           case MessageId.Unchoke:
    //             IsChoked = false;
    //             break;
    //         }
    //       }
    //     }
    //     catch (IOException)
    //     {
    //       IsConnected = false;
    //       Console.WriteLine($"[{IPEndPoint}] Disconnected");
    //     }
    //   }, TaskCreationOptions.LongRunning);
    // }
  }
}