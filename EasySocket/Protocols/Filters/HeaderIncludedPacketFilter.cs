using EasySocket.Protocols.PacketInfos;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace EasySocket.Protocols.Filters
{
    ///<summary>
    /// 헤더가 포함된 패킷의 필터 입니다.
    ///</summary>
    public abstract class HeaderIncludedPacketFilter : IPacketFilter
    {
        protected readonly int headerSize;

        protected int bodySize { get; private set; } = 0;
        protected bool parsedHeader { get; private set; } = false;

        protected HeaderIncludedPacketFilter(int headerSize)
        {
            this.headerSize = headerSize;
        }

        public IPacketInfo Filter(ref SequenceReader<byte> reader)
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
                        return ParsePacketInfo(ref headerSeq, ref headerSeq);
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
                var headerSeq = reader.Sequence.Slice(0, headerSize);
                var bodySeq = reader.Sequence.Slice(headerSize, bodySize);

                return ParsePacketInfo(ref headerSeq, ref bodySeq);
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

        ///<summary>
        /// 헤더 데이터에서 body 사이즈를 파싱합니다.
        ///</summary>
        protected abstract int ParseBodySizeFromHeader(ref ReadOnlySequence<byte> buffer);
        
        
        ///<summary>
        /// 전체 데이터에서 PacketInfo를 파싱합니다.
        ///</summary>
        protected abstract IPacketInfo ParsePacketInfo(ref ReadOnlySequence<byte> headerSeq, ref ReadOnlySequence<byte> bodySeq);
    }
}
