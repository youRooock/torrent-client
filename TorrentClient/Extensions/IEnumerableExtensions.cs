using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TorrentClient.Extensions
{
  public static class EnumerableExtensions
  {
    public static Task ForEachAsync<T>(this IEnumerable<T> source, int maxConcurrency, Func<T, Task> body)
    {
      return Task.WhenAll(Partitioner.Create(source)
        .GetPartitions(maxConcurrency)
        .Select(partition => Task.Run(async () =>
        {
          using (partition)
            while (partition.MoveNext())
              await body(partition.Current);
        })));
    }
  }
}