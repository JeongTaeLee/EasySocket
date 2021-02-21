
using System.Buffers;
using EasySocket.Protocols.PacketInfos;

namespace EasySocket.Protocols.Filters
{
    public interface IPacketFilter
    {
        IPacketInfo Filter(ref SequenceReader<byte> sequence);
        void Reset();
    }
}
