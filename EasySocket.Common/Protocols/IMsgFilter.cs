using System;
using System.Buffers;
namespace EasySocket.Common.Protocols
{
    public interface IMsgFilter
    {
        object Filter(ref ReadOnlySequence<byte> seq);
        void Reset();
    }
}
