using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using TorrentClient.Utils;

namespace TorrentClient
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var totalWatch = Stopwatch.StartNew();
      int maxConcurrency = 10;
      var torrentFileInfo = new TorrentFileInfo(@"D:\ubuntu.torrent");
      var peerId = new byte[20];
      new Random().NextBytes(peerId);
      var tracker = new TorrentTracker(torrentFileInfo.Announce);
      var peers = await tracker.RetrievePeersAsync(torrentFileInfo.InfoHash, peerId, torrentFileInfo.Size);

      var sw = Stopwatch.StartNew();

      var torrectFactory = new TorrentClientFactory(torrentFileInfo.InfoHash, peerId);

      var results = new ConcurrentQueue<PieceResult>();

      var queue = new ConcurrentQueue<WorkItem>();

      var items = torrentFileInfo.PieceHashes.Select((hash, index)
        => new WorkItem(index, hash, CalculatePieceLength(torrentFileInfo, index))).ToList();

      foreach (var item in items)
      {
        queue.Enqueue(item);
      }

      SemaphoreSlim ss = new SemaphoreSlim(15);

      var tasks = new List<Task>();
      foreach (var peer in peers)
      {
        await ss.WaitAsync();
        var t = Task.Run(async () =>
        {
          var s = Stopwatch.StartNew();
          if (!peer.TryConnect())
          {
            Console.WriteLine($"[{peer.IPEndPoint}] failed to connect");
            return;
          }

          // HANDSHAKE
          var handshake = Handshake.Create(torrentFileInfo.InfoHash, peerId);
          peer.Send(handshake.Bytes, false);
          var hs = new byte[68];
          peer.ReadData(hs);

          var handshake2 = Handshake.Parse(hs);

          if (!handshake.Equals(handshake2))
          {
            Console.WriteLine($"[{peer.IPEndPoint}] Handshake failed");
            return;
          }
          else
          {
            Console.WriteLine($"[{peer.IPEndPoint}] successful handshake]");
          }

          // BITFIELD
          var msg = peer.ReadMessage();

          var bf = new Bitfield(msg.Payload);

          // peer.ReadData();

          await peer.SendUnchokeMessage();
          await peer.SendInterestedMessage();


          // DOWNLOAD PIECE

          while (queue.Count != 0)
          {
            if (!queue.TryDequeue(out var item)) continue;
            try
            {
              if (!bf.HasPiece(item.Index))
              {
                Console.WriteLine($"[{peer.IPEndPoint}] doesnt have piece with index {item.Index}");
                queue.Enqueue(item);
                continue;
              }

              var piece = new PieceProgress
              {
                Index = item.Index,
                Buffer = new byte[item.Length]
              };

              long blockSize = 16384;

              Console.WriteLine($"[{peer.IPEndPoint}] Downloading piece {item.Index}");

              for (; piece.Downloaded < item.Length;)
              {
                if (!peer.IsChoked)
                {
                  for (; piece.Requested < item.Length;)
                  {
                    if (item.Length - piece.Requested < blockSize)
                    {
                      blockSize = item.Length - piece.Requested;
                    }

                    peer.SendRequestMessage( // ToDo: reconnect on failure
                      new BlockRequest(item.Index, piece.Requested, blockSize));
                    piece.Requested += blockSize;
                  }
                }

                var mgs = peer.ReadMessage();

                if (mgs == null) continue;

                if (mgs.Id == MessageId.Unchoke) peer.IsChoked = false;

                if (mgs.Id == MessageId.Piece)
                {
                  var n = ParsePiece(piece.Index, piece.Buffer, mgs);

                  piece.Downloaded += n;
                }
              }


              SHA1 sha = new SHA1CryptoServiceProvider();
              var computedHash = sha.ComputeHash(piece.Buffer);
              var eq = item.Hash.SequenceEqual(computedHash);

              if (!eq)
              {
                throw new Exception("failed check sum");
              }

              await peer.SendHaveMessage(item.Index);

              results.Enqueue(new PieceResult {Index = piece.Index, Buffer = piece.Buffer});

              s.Stop();
              Console.WriteLine($"[{peer.IPEndPoint}] Downloaded piece {item.Index} in {s.ElapsedMilliseconds}");
            }
            catch (Exception e)
            {
              queue.Enqueue(item);
              Console.WriteLine("Exception");
              break;
            }
          }

          ss.Release();
        });


        tasks.Add(t);
      }


      await Task.WhenAll(tasks);


      try
      {
        using (var fs = new FileStream(@"D:\my-file.iso", FileMode.Create, FileAccess.Write))
        {
          while (results.Count != 0)
          {
            results.TryDequeue(out var piece);

            var (begin, end) = CalculateBounds(piece.Index, torrentFileInfo);

            fs.Seek(begin, SeekOrigin.Begin);
            fs.Write(piece.Buffer, 0, (int) (end - begin));
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Exception caught in process: {0}", ex);
      }
      
      totalWatch.Stop();
      
      Console.WriteLine($"Downloaded. Total time = {totalWatch.Elapsed.Minutes} mins and {totalWatch.Elapsed.Seconds} secs");
    }


    // foreach (var peer in peers)
    // {
    //   try
    //   {
    //     queue.TryDequeue(out var item);
    //     peer.TryConnect();
    //
    //     var handshake = Handshake.Create(torrentFileInfo.InfoHash, peerId);
    //
    //     peer.Send(handshake.Bytes, false);
    //     var hs = new byte[68];
    //     peer.ReadData(hs);
    //
    //     peer.ReadData();
    //
    //     peer.SendUnchokeMessage();
    //     peer.SendInterestedMessage();
    //
    //     long blockSize = 16384;
    //     long requested = 0;
    //
    //     Console.WriteLine($"Downloading piece {item.Index}");
    //     
    //     for (; requested < item.Length;)
    //     {
    //       if (!peer.IsConnected) break;
    //       
    //       if (!peer.IsChoked)
    //       {
    //         if (item.Length - requested < blockSize)
    //         {
    //           blockSize = item.Length - requested;
    //         }
    //
    //         peer.SendRequestMessage(
    //           new BlockRequest(item.Index, requested, blockSize));
    //         requested += blockSize;
    //         // Console.WriteLine($"Downloaded {requested} of {item.Length}");
    //       }
    //     }
    //     
    //     Console.WriteLine($"Downloaded piece {item.Index}");


    // var cl = torrectFactory.ConnectAsync(peer);
    //
    // if (cl == null)
    // {
    //   continue;
    // }
    //
    // var bf = cl.GetBitmapField();
    //
    // Console.WriteLine($"{peer.IPEndPoint} got bitfield");
    //
    // cl.SendUnchoke();
    // cl.SendInterested();
    //
    // if (!bf.HasPiece(item.Index))
    // {
    //   queue.Enqueue(item);
    // }
    //
    // var buf = cl.TryDownload(item.Index, item.Length);
    //
    // if (buf == null) continue;
    //
    // SHA1 sha = new SHA1CryptoServiceProvider();
    // var computedHash = sha.ComputeHash(buf);
    // var eq = item.Hash.SequenceEqual(computedHash);
    //
    // if (!eq)
    // {
    //   Console.WriteLine("Failed checksum");
    //   continue;
    // }
    //
    //
    // cl.SendHave(item.Index);
    // Console.WriteLine("done");
    // results.Enqueue(new PieceResult {Index = item.Index, Buffer = buf});
    // }
    //   // catch (Exception)
    //   // {
    //   //   Console.WriteLine($"Disconnected {peer.IPEndPoint}");
    //   // }
    // }


    // await Task.WhenAll(tasks);

    // var buffer = new byte[torrentFileInfo.Size];
    // var donePieces = 0;
    //
    //
    // while (donePieces < torrentFileInfo.PieceHashes.Length)
    // {
    //   var r = results.Dequeue();
    //
    //   var begin = r.Index * torrentFileInfo.PieceSize;
    //   var end = begin + torrentFileInfo.PieceSize;
    //
    //   if (end > torrentFileInfo.Size) end = torrentFileInfo.Size;
    //   
    //   Array.Copy(r.Buffer, 0,buffer, begin, end);
    // }
    //
    // sw.Stop();
    //
    // Console.WriteLine(sw.ElapsedMilliseconds);


    static (long, long) CalculateBounds(int index, TorrentFileInfo info)
    {
      var begin = index * info.PieceSize;
      var end = begin + info.PieceSize;

      if (end > info.Size)
      {
        end = info.Size;
      }

      return (begin, end);
    }

    static long CalculatePieceLength(TorrentFileInfo info, int index)
    {
      var begin = index * info.PieceSize;
      var end = begin + info.PieceSize;

      if (end > info.Size)
      {
        end = info.Size;
      }

      return end - begin;
    }

    static int ParsePiece(int index, byte[] buffer, Message msg)
    {
      if (msg.Id != MessageId.Piece)
      {
        return 0;
      }

      if (msg.Payload.Length < 8)
      {
      }

      var parsedIndex = BigEndian.ToUint32(msg.Payload[0..4]);
      if (parsedIndex != index)
      {
      }

      var begin = BigEndian.ToUint32(msg.Payload[4..8]);
      if (begin >= buffer.Length)
      {
      }

      var data = msg.Payload[8..];
      if (begin + data.Length > buffer.Length)
      {
      }

      Array.Copy(data, 0, buffer, begin, data.Length);
      return data.Length;
    }
  }

  public class PieceResult
  {
    public int Index;
    public byte[] Buffer;
  }

  public class WorkItem
  {
    public WorkItem(int index, byte[] hash, long length)
    {
      Index = index;
      Hash = hash;
      Length = length;
    }

    public int Index { get; }
    public byte[] Hash { get; }
    public long Length { get; }
  }
}