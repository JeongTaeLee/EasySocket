using System.Net.Sockets;

namespace EasySocket
{
    public static class SocketExtensions
    {
        public static void SafeClose(this Socket socket)
        {
            if (socket == null)
            {
                return;
            }

            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                socket.Close();
            }
        }
    }
}