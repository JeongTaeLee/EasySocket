using System.Buffers;
using System.Text;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Test
{
    public class StringMsgFilter : IMsgFilter
    {
        public object Filter(ref ReadOnlySequence<byte> sequence)
        {
            return Encoding.Default.GetString(sequence);
        }

        public void Reset()
        {

        }
    }
}