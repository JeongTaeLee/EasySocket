using EasySocket.Common.Protocols.MsgFilters;
using SimpleJSON;
using System;
using System.Buffers;
using System.Text;

namespace ChatServer
{
    public class FixedHeaderJsonMsgFilter : FixedHeaderSizeMsgFilter
    {
        public FixedHeaderJsonMsgFilter() : base(4)
        {

        }

        protected override int ParseBodySizeFromHeader(ref ReadOnlySequence<byte> headerSeq)
        {
            return BitConverter.ToInt32(headerSeq.FirstSpan);
        }

        protected override object ParseMsgInfo(ref ReadOnlySequence<byte> totalSeq)
        {
            var src = Encoding.Default.GetString(totalSeq.Slice(headerSize));
            return JSON.Parse(src);
        }
    }
}
