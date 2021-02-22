
using System.Buffers;
using EasySocket.Protocols.MsgInfos;

namespace EasySocket.Protocols.Filters
{
    /// <summary>
    /// 네트워크에서 받은 데이터를 사용자가 정의한 MsgInfo로 변환하는 클래스
    /// </summary>
    public interface IMsgFilter
    {        
        /// <summary>
        /// Message를 MsgInfo로 변경합니다.
        /// </summary>
        IMsgInfo Filter(ref SequenceReader<byte> sequence);

        /// <summary>
        /// 다음 파싱을 위해 파싱에 필요했던 데이터를 초기화
        /// </summary>
        void Reset();
    }
}
