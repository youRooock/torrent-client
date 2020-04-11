using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class Connection : IDisposable
  {
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _ns;
    private readonly BinaryWriter _writer;
    private readonly BinaryReader _reader;

    public Connection(IPEndPoint endPoint)
    {
      _tcpClient = new TcpClient {SendTimeout = 3000, ReceiveTimeout = 3000};
      _tcpClient.Connect(endPoint.Address, endPoint.Port);
      _ns = _tcpClient.GetStream();
      _writer = new BinaryWriter(_ns);
      _reader = new BinaryReader(_ns);
    }

    public void Write(byte[] arr)
    {
      _writer.Write(arr);
      _writer.Flush();
    }

    public void Read(byte[] arr)
    {
      var offset = 0;
      var remaining = arr.Length;
      do
      {
        var readBytes = _reader.Read(arr, offset, remaining);

        if (readBytes == 0)
        {
          break;
        }

        remaining -= readBytes;
        offset += readBytes;
      } while (remaining > 0);
    }
    
    public byte[] Read(int count)
    {
      var offset = 0;
      var buff = new byte[count];
      var remaining = count;
      do
      {
        var readBytes = _reader.Read(buff, offset, remaining);

        if (readBytes == 0)
        {
          break;
        }

        remaining -= readBytes;
        offset += readBytes;
      } while (remaining > 0);

      return buff;
    }

    public int Read()
    {
      return _reader.ReadInt32();
    }

    public void Dispose()
    {
      _tcpClient.Close();
      _ns.Dispose();
    }
  }
}