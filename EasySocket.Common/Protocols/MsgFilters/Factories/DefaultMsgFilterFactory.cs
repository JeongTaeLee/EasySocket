namespace EasySocket.Common.Protocols.MsgFilters.Factories
{
    public class DefaultMsgFilterFactory<TMsgFilter, TPacket> : IMsgFilterFactory<TPacket>
        where TMsgFilter : IMsgFilter<TPacket>, new()
    {
        public IMsgFilter<TPacket> Get()
        {
            return new TMsgFilter();
        }
    }
}