﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Schema;
using TorrentClient.Utils;

namespace TorrentClient
{
  class Program
  {
    static Channel<PieceResult> fileChannel = Channel.CreateUnbounded<PieceResult>();

    static async Task Main(string[] args)
    {
      var totalWatch = Stopwatch.StartNew();
      int maxConcurrency = 10;
      var torrentFileInfo = new TorrentFileInfo(@"D:\ubuntu.torrent");
      var consumer = Task.Run(async () => await ConsumeAsync(torrentFileInfo));

      var peerId = new byte[20];
      new Random().NextBytes(peerId);
      var tracker = new TorrentTracker(torrentFileInfo.Announce);
      var endpoints = await tracker.RetrievePeersAsync(torrentFileInfo.InfoHash, peerId, torrentFileInfo.Size);

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

      SemaphoreSlim ss = new SemaphoreSlim(10);

      var tasks = new List<Task>();
      foreach (var endpoint in endpoints)
      {
        await ss.WaitAsync();
        var t = Task.Run(async () =>
        {
          var peer = Peer.Create(endpoint);

          var s = Stopwatch.StartNew();
          if (!peer.TryConnect(torrentFileInfo.InfoHash, peerId))
          {
            Console.WriteLine($"[{peer.IPEndPoint}] failed to connect");
            return;
          }

          peer.SendUnchokeMessage();
          peer.SendInterestedMessage();

          while (queue.Count != 0)
          {
            if (!queue.TryDequeue(out var item)) continue;
            try
            {
              if (!peer.Bitfield.HasPiece(item.Index))
              {
                Console.WriteLine($"[{peer.IPEndPoint}] doesnt have piece with index {item.Index}");
                queue.Enqueue(item);
                break;
              }

              var piece = new Piece
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

                    peer.SendRequestMessage(
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

              piece.CheckIntegrity(item.Hash);
              peer.SendHaveMessage(item.Index);

              await PublishAsync(new PieceResult {Index = piece.Index, Buffer = piece.Buffer});

              s.Stop();
              Console.WriteLine($"[{peer.IPEndPoint}] Downloaded piece {item.Index} in {s.ElapsedMilliseconds}");
            }
            catch (Exception e)
            {
              queue.Enqueue(item);
              Console.WriteLine($"[{peer.IPEndPoint}] Disconnected");
              break;
            }
          }

          ss.Release();
        });

        tasks.Add(t);
      }

      while (!fileChannel.Writer.TryComplete())
      {
      }

      tasks.Add(consumer);
      await Task.WhenAll(tasks);
      totalWatch.Stop();

      Console.WriteLine(
        $"Downloaded. Total time = {totalWatch.Elapsed.Minutes} mins and {totalWatch.Elapsed.Seconds} secs");
    }

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
        }
      }
    }

    static async Task PublishAsync(PieceResult piece)
    {
      await fileChannel.Writer.WriteAsync(piece);
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