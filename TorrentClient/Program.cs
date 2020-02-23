using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TorrentClient.Utils;

namespace TorrentClient
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var torrentFileInfo = new TorrentFileInfo("/Users/yourock/Downloads/ubuntu-19.10-desktop-amd64.iso.torrent");
      var peerId = new byte[20];
      new Random().NextBytes(peerId);
      var tracker = new TorrentTracker(torrentFileInfo.Announce);
      var peers = await tracker.RetrievePeersAsync(torrentFileInfo.InfoHash, peerId, torrentFileInfo.Size);
      var availablePeers = new List<Peer>();


      var tasks = new List<Task>();
      var sw = Stopwatch.StartNew();

      //Parallel.ForEach(peers, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async peer =>
      //{
      //  var cl = new TorrentClient(torrentFileInfo.InfoHash, peerId);
      //  await cl.ConnectAsync(peer);
      //});

      foreach (var peer in peers)
      {
        try
        {
          //var cl = new TorrentClient(torrentFileInfo.InfoHash, peerId);
          //await cl.ConnectAsync(peer);


          tasks.Add(Task.Run(async () =>
          {
            var cl = new TorrentClientFactory(torrentFileInfo.InfoHash, peerId);
            await cl.ConnectAsync(peer);
          }));
          // await cl.ConnectAsync(peer);

          //var tcp = new TcpClient();

          //tcp.Connect(peer.IPEndPoint);

          //var tcpStream = tcp.GetStream();
          //var handshake = Handshake.Create(torrentFileInfo.InfoHash, peerId);

          //tcpStream.Write(handshake.Bytes, 0, handshake.Bytes.Length);

          //var resp = new byte[tcp.ReceiveBufferSize];
          //var bytes = tcpStream.Read(resp, 0, tcp.ReceiveBufferSize);
          //var handshakeResponse = Handshake.Parse(resp);

          //if (handshake.Equals(handshakeResponse))
          //{


          //  byte[] bitfieldLength = new byte[4];
          //  tcpStream.Read(bitfieldLength, 0, 4);
          //  var length = BigEndian.ToUint32(bitfieldLength);
          //  var bitfield = new byte[length];

          //  tcpStream.Read(bitfield, 0, bitfield.Length);



          //  availablePeers.Add(peer);

          //  SendUnchoke(tcpStream);
          //  SendInterested(tcpStream);

          //byte[] bitfieldLength = new byte[4];

          //tcpStream.Read(bitfieldLength, 0, 4);

          //var length = BigEndian.ToUint32(bitfieldLength);

          //var bitfield = new byte[length];

          //tcpStream.Read(bitfield, 0, bitfield.Length);
          //}
        }

        catch (Exception e)
        {
          continue;
        }
      }

      try
      {
        await Task.WhenAll(tasks);
      }

      catch(Exception e)
      {
        Console.WriteLine(e.ToString());
      }
      sw.Stop();

      Console.WriteLine(sw.ElapsedMilliseconds);
    }

    static void SendUnchoke(NetworkStream stream)
    {
      var mgs = new Message { Id = MessageId.Unchoke };
      var arr = new byte[5];

      var arr1 = new byte[] { 0, 0, 0, 0, 0 }; // first 4 bytes indicates length


      BigEndian.PutUint32(arr1, 1);

      arr1[4] = (byte)MessageId.Unchoke;

      stream.Write(arr1, 0, 5);
    }
    static void SendInterested(NetworkStream stream)
    {
      var arr1 = new byte[] { 0, 0, 0, 0, 0 }; // first 4 bytes indicates length


      BigEndian.PutUint32(arr1, 1);

      arr1[4] = (byte)MessageId.Interested;

      stream.Write(arr1, 0, 5);
    }
  }
}
