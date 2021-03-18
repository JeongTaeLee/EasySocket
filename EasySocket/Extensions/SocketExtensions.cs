using System.Net.Sockets;

namespace EasySocket
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
    }
}