using System;
using System.Buffers;

namespace EasySocket.Common.Protocols
{
    public abstract class FixedHeaderSizeMsgFilter : IMsgFilter
    {
        protected readonly int headerSize;

        protected int bodySize { get; private set; } = 0;

        protected bool parsedHeader { get; private set; } = false;

        protected FixedHeaderSizeMsgFilter(int headerSize)
        {
            this.headerSize = headerSize;
        }

        public object Filter(ref ReadOnlySequence<byte> seq)
        {
            if (!parsedHeader)
            {
                parsedHeader = InternalParseBodySize(ref seq);

                if (!parsedHeader)
                {
                    return null;
                }
            }

            return InternalParseBody(ref seq);
        }

        private bool InternalParseBodySize(ref ReadOnlySequence<byte> seq)
        {
            if (headerSize > seq.Length)
            {
                return false;
            }

            var headerSeq = seq.Slice(0, headerSize);

            bodySize = ParseBodySizeFromHeader(ref headerSeq);
            if (0 > bodySize)
            {
                throw new ProtocolException("The body size cannot be smaller than 0.");
            }

            return true;
        }

        private object InternalParseBody(ref ReadOnlySequence<byte> sequence)
        {
            if (!parsedHeader)
            {
                throw new InvalidOperationException("The header was not parsed.");
            }

            try
            {
                if (0 == bodySize)
                {
                    var headerSeq = sequence.Slice(0, headerSize);
                    sequence = sequence.Slice(headerSize);

                    return ParseMsgInfo(ref headerSeq);
                }
                else
                {
                    if (sequence.Length - headerSize < bodySize)
                    {
                        return null;
                    }

                    var totalSize = headerSize + bodySize;
                    var totalSeq = sequence.Slice(0, totalSize);

                    sequence = sequence.Slice(sequence.GetPosition(totalSize));

                    return ParseMsgInfo(ref totalSeq);
                }
            }
            finally
            {
                this.Reset();
            }
        }

        public void Reset()
        {
            bodySize = 0;
            parsedHeader = false;
        }

        protected abstract int ParseBodySizeFromHeader(ref ReadOnlySequence<byte> headerSeq);

        protected abstract object ParseMsgInfo(ref ReadOnlySequence<byte> totalSeq);

    }
}