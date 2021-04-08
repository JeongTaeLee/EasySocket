using System.Buffers;
using System.Text;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Test
{
    public class StringMsgFilter : IMsgFilter
    {
        public object Filter(ref SequenceReader<byte> sequence)
        {
            try
            {
                return Encoding.Default.GetString(sequence.Sequence.Slice(0, sequence.Length));
            }
            finally
            {
                sequence.Advance(sequence.Length);
            }
        }

        public void Reset()
        {

        }
    }
}