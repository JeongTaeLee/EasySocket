using EasySocket.Protocols.PacketInfos;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace EasySocket.Protocols.Filters
{
    public abstract class FixedHeaderSizePacketFilter : IPacketFilter
    {
        protected readonly int headerSize;

        protected int bodySize { get; private set; } = 0;
        protected bool parsedHeader { get; private set; } = false;

        protected FixedHeaderSizePacketFilter(int headerSize)
        {
            this.headerSize = headerSize;
        }

        public IPacketInfo Filter(SequenceReader<byte> reader)
        {
            if (!parsedHeader)
            {
                if (headerSize > reader.Length)
                {
                    return null;
                }

                var headerSeq = reader.Sequence.Slice(0, headerSize);

                // 헤더 파싱 시작 - body 사이즈 가져 오기.
                bodySize = ParseBodySizeFromHeader(ref headerSeq);
                if (0 >= bodySize)
                {
                    throw new ProtocolException("The body size cannot be smaller than 0.");
                }

                try
                {
                    if (bodySize == 0)
                    {
                        return ParsePacketInfoFromTotal(ref headerSeq);
                    }
                }
                finally
                {
                    // 헤더로만 파싱 완료.
                    reader.Advance(headerSize);
                }

                parsedHeader = true;
            }

            if (bodySize > reader.Length - headerSize)
            {
                return null;
            }

            try
            {
                var totalSeq = reader.Sequence.Slice(0, headerSize + bodySize);
                return ParsePacketInfoFromTotal(ref totalSeq);
            }
            finally
            {
                reader.Advance(bodySize);
                Reset();
            }
        }

        public void Reset()
        {
            bodySize = 0;
            parsedHeader = false;
        }

        protected abstract int ParseBodySizeFromHeader(ref ReadOnlySequence<byte> buffer);
        protected abstract IPacketInfo ParsePacketInfoFromTotal(ref ReadOnlySequence<byte> buffer);
    }
}
