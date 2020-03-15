using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TorrentClient
{
  public class Connection: IDisposable
  {
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _ns;
    

    public Connection(IPEndPoint endPoint)
    {
      _tcpClient = new TcpClient { NoDelay = true, ReceiveTimeout = 3000, SendTimeout = 3000};

      var result = _tcpClient.BeginConnect(endPoint.Address, endPoint.Port, null, null);
      var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));

      if (!success)
      {
        Console.WriteLine($"Couldn't connect {endPoint}");
        throw new Exception($"Couldn't connect {endPoint}");
      }

      _tcpClient.EndConnect(result);

      _ns = _tcpClient.GetStream();
      _ns.ReadTimeout = 3000;
      _ns.WriteTimeout = 3000;
    }

    public void Write(byte[] arr)
    {
      _ns.Write(arr);
      _ns.Flush();
    }

    public void Read(byte[] arr)
    {
      var offset = 0;
      var read = int.MaxValue;
      var remaining = arr.Length;
      while (remaining > 0 && read > 0)
      {
        read = _ns.Read(arr, offset, remaining);
        remaining -= read;
        offset += read;
      }
    }

    public void Dispose()
    {
      _tcpClient.Close();
      _ns.Dispose();
      //_writer.Dispose();
      //_reader.Dispose();
    }
  }
}
