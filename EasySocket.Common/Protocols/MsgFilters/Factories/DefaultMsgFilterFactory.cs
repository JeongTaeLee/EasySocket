namespace EasySocket.Common.Protocols.MsgFilters.Factories
{
    public class DefaultMsgFilterFactory<TMsgFilter> : IMsgFilterFactory
        where TMsgFilter : class, IMsgFilter, new()
    {
        public IMsgFilter Get()
        {
            return new TMsgFilter();
        }
    }
}