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
      var payload = new byte[12].AsSpan();
      BigEndian.PutUint32(payload.Slice(0, 4), request.Index);
      BigEndian.PutUint32(payload.Slice(4, 4), request.Offset);
      BigEndian.PutUint32(payload.Slice(8, 4), request.Length);

      var indx = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(request.Index));
      var offset = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(request.Offset));
      var len = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(request.Length));

      var payload2 = indx.Concat(offset).Concat(len);
      
      using var ms = new MemoryStream();
      var bw = new BinaryWriter(ms);

      var length = new byte[4];

      BigEndian.PutUint32(length, payload.Length + 1);

      bw.Write(length);
      bw.Write((byte)MessageId.Request);
      bw.Write(payload);

      Send(ms.ToArray(), false);
    }

    public void SendUnchokeMessage()
    {
      var bytes = new UnchokeMessage().Serialize();

      Send(bytes, false);
    }

    public void SendInterestedMessage()
    {
     var bytes = new InterestedMessage().Serialize();

      Send(bytes, false);
    }

    public async Task SendHaveMessage(long index)
    {
      using var ms = new MemoryStream();
      BinaryWriter w = new BigEndianBinaryWriter(ms);
      var message = new HaveMessage(w);
      message.Send(index);
      await Send(ms.ToArray());
    }

    public async Task Send(byte[] data)
    {
      Connection.BinaryWriter.Write(data.Length);
      Connection.BinaryWriter.Write(data);
      Connection.BinaryWriter.Flush();
      await Data.Writer.WriteAsync(data);
    }

    public void Send(byte[] data, bool s)
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

    public void ReadData(byte[] arr)
    {
      Connection.Read(arr);
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
      int length = Connection.BinarReader.ReadInt32();

      if (length == 0) return null;

      byte[] data = Connection.BinarReader.ReadBytes(length);

      var msg = new Message(data);

      return msg;
    }


    public void ReadData()
    {
      Task.Factory.StartNew(() =>
      {
        try
        {
          while (true)
          {
            // Read message length
            int length = Connection.BinarReader.ReadInt32();

            if (length == 0)
            {
              Console.WriteLine($"[{IPEndPoint}] keep alive");
              continue;
            }

            // Read data
            byte[] data = Connection.BinarReader.ReadBytes(length);

            var m = new Message(data);

            switch (m.Id)
            {
              case MessageId.Piece:
                break;
              case MessageId.Unchoke:
                IsChoked = false;
                break;
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
}