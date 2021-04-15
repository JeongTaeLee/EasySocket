using System.Buffers;

namespace EasySocket.Common.Protocols.MsgFilters
{
    /// <summary>
    /// 네트워크에서 받은 데이터를 사용자가 정의한 MsgInfo로 변환하는 클래스
    /// </summary>
    public interface IMsgFilter
    {
        object Filter(ref SequenceReader<byte> sequence);

        /// <summary>
        /// 파싱이 끝나면 Reset하는 함수 입니다.
        /// </summary>
        void Reset();
    }
}
