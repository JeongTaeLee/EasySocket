namespace EasySocket.Common.Protocols.MsgFilters.Factories
{
    public interface IMsgFilterFactory<TPacket>
    {
        IMsgFilter<TPacket> Get();
    }
}