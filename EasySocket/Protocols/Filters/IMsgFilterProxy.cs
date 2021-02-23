using System;
using System.Buffers;
using System.Net.Sockets;
using EasySocket.Protocols.MsgInfos;

namespace EasySocket.Protocols.Filters
{
    /// <summary>
    /// Message를 파싱하는 Filter를 작동 시키는 클래스
    /// </summary>
    public class MsgFilterProxy
    {
        private IMsgFilter filter = null;

        private int position = 0;
        
        public MsgFilterProxy(IMsgFilter filter)
        {
            this.filter = filter;
        }

        void Filter(SequenceReader<byte> sequenceReader, Action<IMsgInfo> onFiltered)
        {

            while (true)
            {
                
            }
        }
    }
}