namespace EasySocket.Protocols.PacketInfos
{
    public class StringPacketInfo : IPacketInfo
    {
        private string _str;
        public string str => _str;

        public StringPacketInfo(string str)
        {
            _str = str;
        }
    }
}
