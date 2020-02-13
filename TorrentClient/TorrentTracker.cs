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

    public async Task<List<Peer>> RetrievePeers(byte[] hashInfo, long size)
    {
      var peers = new List<Peer>();

      var trackerResponse = await _announce
        .SetQueryParam("info_hash", HttpUtility.UrlEncode(hashInfo), true)
        .SetQueryParam("peer_id", HttpUtility.UrlEncode(GetRandomPeerId()), true)
        .SetQueryParam("port", 8861)
        .SetQueryParam("uploaded", "0")
        .SetQueryParam("downloaded", "0")
        .SetQueryParam("compact", "1")
        .SetQueryParam("left", size)
        .AllowHttpStatus("2xx")
        .GetAsync();

      var content = await trackerResponse.Content.ReadAsStreamAsync();

      var e = (_parser.Parse(content).ToKeyValuePairs().FirstOrDefault(r => r.Key == "peers").Value as BString).Value;

      for (int i = 0; i < e.Length; i += 6)
      {
        peers.Add(new Peer(new IPAddress(e.Slice(i, 4).Span), BigEndian.ToUint16(e.Slice(i + 4, 2).Span)));
      }

      return peers;
    }

    private byte[] GetRandomPeerId()
    {
      var peerId = new byte[20];
      new Random().NextBytes(peerId);

      return peerId;
    }
  }
}
