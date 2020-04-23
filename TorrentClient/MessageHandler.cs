using System;
using TorrentClient.Events;
using TorrentClient.Messages;
using TorrentClient.Utils;

namespace TorrentClient
{
  public class MessageHandler
  {
    public event Action<BitfieldEventArgs> OnBitfieldReceived;
    public event Action<PieceEventArgs> OnPieceReceived;
    public event Action OnUnchokeReceived;
    public event Action OnChokeReceived;
    public event Action<HaveEventArgs> OnHaveReceived;


    public void Handle(ResponseMessage message)
    {
      switch (message.Id)
      {
        case MessageId.Bitfield:
          OnBitfieldReceived?.Invoke(
            new BitfieldEventArgs {Bitfield = new Bitfield(message.Payload)}
          );
          break;
        case MessageId.Piece:
          OnPieceReceived?.Invoke(new PieceEventArgs
            {Payload = message.Payload}
          );
          break;
        case MessageId.Choke:
          OnChokeReceived?.Invoke();
          break;
        case MessageId.Unchoke:
          OnUnchokeReceived?.Invoke();
          break;
        case MessageId.Have:
          OnHaveReceived?.Invoke(new HaveEventArgs { Index = BigEndian.ToUint32(message.Payload) });
          break;
      }
    }
  }
}