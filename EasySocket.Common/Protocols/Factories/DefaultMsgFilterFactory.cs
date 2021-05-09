namespace EasySocket.Common.Protocols.Factories
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