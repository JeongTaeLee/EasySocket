using System.Net;
using System.Net.Sockets;

namespace EasySocket.Common.Extensions
{
    public static class SocketExtensions
    {
        public static void SafeClose(this Socket sckt)
        {
            if (sckt == null)
            {
                return;
            }

            try
            {
                sckt.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                sckt.Close();
            }
        }

        public static IPAddress ToIPAddress(this string strIp)
        {
            if (strIp == "Any")
            {
                return IPAddress.Any;
            }

            return IPAddress.Parse(strIp);
        }
    }
}