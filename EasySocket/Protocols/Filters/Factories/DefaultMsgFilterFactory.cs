namespace EasySocket.Protocols.Filters.Factories
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