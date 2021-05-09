namespace EasySocket.Common.Protocols.MsgFilters.Factories
{
    public class DefaultMsgFilterFactory<TMsgFilter> : IMsgFilterFactory
        where TMsgFilter : IMsgFilter, new()
    {
        public IMsgFilter Get()
        {
            return new TMsgFilter();
        }
    }
}