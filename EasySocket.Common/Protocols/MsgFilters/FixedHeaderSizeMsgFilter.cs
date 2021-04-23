using System.Buffers;

namespace EasySocket.Common.Protocols.MsgFilters
{
    ///<summary>
    /// Body 부분 사이즈를 고정된 사이즈의 헤더에 포함하는 Msg 필터
    /// Message 구조 = Header(width BodySize) + Body
    ///</summary>
    public abstract class FixedHeaderSizeMsgFilter : IMsgFilter
    {
        /// <summary>
        /// 고정된 헤더 사이즈
        /// </summary>
        protected readonly int headerSize;

        /// <summary>
        /// 헤더에서 파싱한 Body 사이즈
        /// </summary>
        protected int bodySize { get; private set; } = 0;

        /// <summary>
        /// 헤더가 파싱됬는지를 구분하는 플라그 (Ture : 파싱됨)
        /// </summary>
        protected bool parsedHeader { get; private set; } = false;

        protected FixedHeaderSizeMsgFilter(int headerSize)
        {
            this.headerSize = headerSize;
        }

        public object Filter(ref SequenceReader<byte> reader)
        {
            if (!parsedHeader)
            {
                if (headerSize > reader.Length)
                {
                    return null;
                }

                var headerSeq = reader.Sequence.Slice(0, headerSize);
                
                // 파싱한 만큼 포지션을 이동
                reader.Advance(headerSize);

                bodySize = ParseBodySizeFromHeader(ref headerSeq);
                if (0 >= bodySize)
                {
                    throw new ProtocolException("The body size cannot be smaller than 0.");
                }

                if (bodySize == 0)
                {
                    try
                    {
                        return ParseMsgInfo(ref headerSeq, ref headerSeq);
                    }
                    finally
                    {
                        Reset();
                    }
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

                // 파싱한 만큼 포지션을 이동
                reader.Advance(bodySize);
                
                return ParseMsgInfo(ref headerSeq, ref bodySeq);
            }
            finally
            {
                Reset();
            }
        }

        public void Reset()
        {
            bodySize = 0;
            parsedHeader = false;
        }

        ///<summary>
        /// Message Header 부분에서 body의 사이즈를 파싱합니다.
        ///</summary>
        protected abstract int ParseBodySizeFromHeader(ref ReadOnlySequence<byte> buffer);
        
        ///<summary>
        /// Message Body 부분에서 IMsgInfo를 파싱합니다.
        ///</summary>
        protected abstract object ParseMsgInfo(ref ReadOnlySequence<byte> headerSeq, ref ReadOnlySequence<byte> bodySeq);

    }
}
