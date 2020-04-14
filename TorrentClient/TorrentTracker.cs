using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using Flurl;
using Flurl.Http;
using Flurl.Util;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class TorrentTracker
  {
    private readonly BencodeParser _parser;
    private readonly string _announce;

    public TorrentTracker(string announce)
    {
      _parser = new BencodeParser();
      _announce = announce;
    }

    public async Task<List<IPEndPoint>> RetrievePeersAsync(byte[] hashInfo, byte[] peerId, long size)
    {
      var peers = new List<IPEndPoint>();

      var trackerResponse = await _announce
        .SetQueryParam("info_hash", HttpUtility.UrlEncode(hashInfo), true)
        .SetQueryParam("peer_id", HttpUtility.UrlEncode(peerId), true)
        .SetQueryParam("port", 8861)
        .SetQueryParam("uploaded", "0")
        .SetQueryParam("downloaded", "0")
        .SetQueryParam("compact", "1")
        .SetQueryParam("left", size)
        .AllowHttpStatus("2xx")
        .GetAsync();

      var contentStream = await trackerResponse.Content.ReadAsStreamAsync();

      var peersArray = (
        _parser.Parse(contentStream)
          .ToKeyValuePairs().FirstOrDefault(r => r.Key == "peers")
          .Value as BString
        )?.Value ?? throw new InvalidOperationException("Could find peers array in tracker response");


      for (int i = 0; i < peersArray.Length; i += 6)
      {
        peers.Add(new IPEndPoint(new IPAddress(peersArray.Slice(i, 4).Span), BigEndian.ToUint16(peersArray.Slice(i + 4, 2).Span)));
      }

      return peers;
    }
  }
}
