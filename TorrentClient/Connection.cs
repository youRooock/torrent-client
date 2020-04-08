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
    public BigEndianBinaryWriter BinaryWriter { get; }
    public BigEndianBinaryReader BinarReader { get; }


    public Connection(IPEndPoint endPoint)
    {
      _tcpClient = new TcpClient { SendTimeout = 3000, ReceiveTimeout = 3000};

      _tcpClient.Connect(endPoint.Address, endPoint.Port);

      _ns = _tcpClient.GetStream();

      _writer = new BinaryWriter(_ns);
      _reader = new BinaryReader(_ns);
      BinaryWriter = new BigEndianBinaryWriter(_ns);
      BinarReader = new BigEndianBinaryReader(_ns);
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

    public void Dispose()
    {
      _tcpClient.Close();
      _ns.Dispose();
    }
  }
}