using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace TorrentClient
{
  public class Connection : IDisposable
  {
    private readonly TcpClient _tcpClient;
    private readonly BinaryWriter _writer;
    private readonly BinaryReader _reader;

    public Connection(IPEndPoint endPoint)
    {
      _tcpClient = new TcpClient {SendTimeout = 3000, ReceiveTimeout = 3000};
      _tcpClient.Connect(endPoint.Address, endPoint.Port);
      var ns = _tcpClient.GetStream();
      _writer = new BinaryWriter(ns);
      _reader = new BinaryReader(ns);
    }

    public void Write(byte[] arr)
    {
      _writer.Write(arr);
      _writer.Flush();
    }

    public byte[] Read(int count)
    {
      return _reader.ReadBytes(count);
    }

    public int ReadSize()
    {
      return _reader.ReadInt32();
    }

    public void Dispose()
    {
      _tcpClient.Close();
    }
  }
}