
using System.Buffers;
using EasySocket.Common.Protocols.MsgInfos;

namespace EasySocket.Common.Protocols.MsgFilters
{
    /// <summary>
    /// 네트워크에서 받은 데이터를 사용자가 정의한 MsgInfo로 변환하는 클래스
    /// </summary>
    public interface IMsgFilter
    {
        /// <summary>
        /// 수신한 데이터를 <see cref ="IMsgInfo"/>로 변환 합니다.
        /// </summary>
        /// <param name="msgInfo">파싱된 <see cref="IMsgInfo"/>을 담을 out 변수입니다.</param>
        /// <param name="sequence">파싱할 데이터가 들어있는 <see cref="SequenceReader"/> 입니다.</param>
        /// <returns>파싱할때 사용한 데이터 길이 입니다.</returns>
        IMsgInfo Filter(ref SequenceReader<byte> sequence);

        /// <summary>
        /// 파싱이 끝나면 Reset하는 함수 입니다.
        /// </summary>
        void Reset();
    }
}
