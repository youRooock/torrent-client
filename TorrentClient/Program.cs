using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TorrentClient.Messages;
using TorrentClient.Utils;

namespace TorrentClient
{
  class Program
  {
    static Channel<Piece> fileChannel = Channel.CreateUnbounded<Piece>();

    static async Task Main(string[] args)
    {
      var totalWatch = Stopwatch.StartNew();
      var torrentFileInfo = new TorrentFileInfo(@"D:\ubuntu.torrent");
      var consumer = Task.Run(async () => await ConsumeAsync(torrentFileInfo));

      var tracker = new TorrentTracker(torrentFileInfo.Announce);
      var endpoints = await tracker.RetrievePeersAsync(torrentFileInfo.InfoHash, PeerId.CreateNew(), torrentFileInfo.Size);

      var peers = endpoints.Select(Peer.Create);

      var queue = new ConcurrentQueue<RequestItem>();

      // var items = torrentFileInfo.PieceHashes.Select((hash, index)
      //   => new WorkItem(index, hash, CalculatePieceLength(torrentFileInfo, index))).ToList();
      torrentFileInfo.PieceHashes.Select((hash, index)
        => new RequestItem(index, hash, CalculatePieceLength(torrentFileInfo, index))).ToList().ForEach(r => queue.Enqueue(r));
      

     var torrentDownloader = new TorrentDownloader(peers, fileChannel.Writer, torrentFileInfo, queue);
     
     torrentDownloader.Download();

      #region MyRegion
      
      // foreach (var item in items)
      // {
      //   queue.Enqueue(item);
      // }
      //
      // SemaphoreSlim ss = new SemaphoreSlim(10);
      //
      // var tasks = new List<Task>();
      // foreach (var endpoint in endpoints)
      // {
      //   await ss.WaitAsync();
      //   if (Downloaded == torrentFileInfo.Size) return;
      //
      //   var t = Task.Run(async () =>
      //   {
      //     using var peer = Peer.Create(endpoint);
      //
      //     var s = Stopwatch.StartNew();
      //     if (!peer.TryConnect(torrentFileInfo.InfoHash))
      //     {
      //       Console.WriteLine($"[{peer.IPEndPoint}] failed to connect");
      //       return;
      //     }
      //
      //     peer.SendMessage(new UnchokeMessage());
      //     peer.SendMessage(new InterestedMessage());
      //
      //     while (queue.Count != 0)
      //     {
      //       if (!queue.TryDequeue(out var item)) continue;
      //       try
      //       {
      //         if (!peer.Bitfield.HasPiece(item.Index))
      //         {
      //           Console.WriteLine($"[{peer.IPEndPoint}] doesnt have piece with index {item.Index}");
      //           queue.Enqueue(item);
      //           break;
      //         }
      //
      //         var piece = new Piece
      //         {
      //           Index = item.Index,
      //           Buffer = new byte[item.Length]
      //         };
      //
      //         long blockSize = 16384;
      //
      //         Console.WriteLine($"[{peer.IPEndPoint}] Downloading piece {item.Index}");
      //
      //         for (; piece.Downloaded < item.Length;)
      //         {
      //           if (!peer.IsChoked)
      //           {
      //             for (; piece.Requested < item.Length;)
      //             {
      //               if (item.Length - piece.Requested < blockSize)
      //               {
      //                 blockSize = item.Length - piece.Requested;
      //               }
      //
      //               peer.SendMessage(new RequestMessage(new PieceBlock(item.Index, piece.Requested, blockSize)));
      //               piece.Requested += blockSize;
      //             }
      //           }
      //
      //           var mgs = peer.ReadMessage();
      //
      //           if (mgs == null) continue;
      //
      //           if (mgs.Id == MessageId.Unchoke) peer.IsChoked = false;
      //
      //           if (mgs.Id == MessageId.Piece)
      //           {
      //             var n = ParsePiece(piece.Index, piece.Buffer, mgs);
      //
      //             piece.Downloaded += n;
      //           }
      //         }
      //
      //         piece.CheckIntegrity(item.Hash);
      //         peer.SendMessage(new HaveMessage(item.Index));
      //
      //         await PublishAsync(new PieceResult {Index = piece.Index, Buffer = piece.Buffer});
      //
      //         s.Stop();
      //         Console.WriteLine($"[{peer.IPEndPoint}] Downloaded piece {item.Index} in {s.ElapsedMilliseconds}");
      //       }
      //       catch (Exception)
      //       {
      //         queue.Enqueue(item);
      //         Console.WriteLine($"[{peer.IPEndPoint}] Disconnected");
      //         break;
      //       }
      //     }
      //
      //     ss.Release();
      //   });
      //
      //   tasks.Add(t);
      // }
      //
      // fileChannel.Writer.Complete();
      //
      // tasks.Add(consumer);
      // await Task.WhenAll(tasks);
      // totalWatch.Stop();
      //
      // Console.WriteLine(
      //   $"Downloaded. Total time = {totalWatch.Elapsed.Minutes} mins and {totalWatch.Elapsed.Seconds} secs");
      
      #endregion
    }

    private static long Downloaded = 0;

    static async Task ConsumeAsync(TorrentFileInfo torrentFileInfo)
    {
      await using var fs = new FileStream(@"D:\my-file.iso", FileMode.Create, FileAccess.Write);
      while (await fileChannel.Reader.WaitToReadAsync())
      {
        if (fileChannel.Reader.TryRead(out var piece))
        {
          var (begin, end) = CalculateBounds(piece.Index, torrentFileInfo);
          fs.Seek(begin, SeekOrigin.Begin);
          fs.Write(piece.Buffer, 0, (int) (end - begin));

          Interlocked.Add(ref Downloaded, piece.Buffer.Length);
        }
      }
    }

    static async Task PublishAsync(PieceResult piece)
    {
      // await fileChannel.Writer.WriteAsync(piece);
    }

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