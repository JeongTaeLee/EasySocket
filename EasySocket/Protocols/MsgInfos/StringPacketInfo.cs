namespace EasySocket.Protocols.MsgInfos
{
    public class StringMsgInfo : IMsgInfo
    {
        private string _str;
        public string str => _str;

        public StringMsgInfo(string str)
        {
            _str = str;
        }
    }
}
