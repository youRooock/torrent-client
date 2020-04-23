using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace TorrentClient
{
  public class Connection : IDisposable
  {
    private readonly TcpClient _tcpClient;
    public BinaryWriter Writer { get; }
    public BinaryReader Reader { get; }

    public Connection(IPEndPoint endPoint)
    {
      _tcpClient = new TcpClient {SendTimeout = 3000, ReceiveTimeout = 3000};
      _tcpClient.Connect(endPoint.Address, endPoint.Port);
      var ns = _tcpClient.GetStream();
      Writer = new BinaryWriter(ns);
      Reader = new BinaryReader(ns);
    }

    public void Dispose()
    {
      _tcpClient.Dispose();
    }
  }
}