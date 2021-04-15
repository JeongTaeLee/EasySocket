using EasySocket.Server.Listeners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EasySocket.Test
{
    // @NOTE: Extension 클래스에는 확장 메서드만 존재하게 한다.
    public static class TestHelper
    {
        // TODO - 위치 맘에 안들면 옮기세요
        public static int GetFreePort(string ipAddress)
        {
            Socket portFinder = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                portFinder.Bind(new IPEndPoint(IPAddress.Parse(ipAddress), 0));
                return ((IPEndPoint)portFinder.LocalEndPoint).Port;
            }
            catch
            {
                throw;
            }
            finally
            {
                portFinder.Close();
            }
        }

        public static int GetLocalFreePort()
        {
            return GetFreePort("127.0.0.1");
        }

        public static ListenerConfig GetLocalListenerConfig(int backlog = 1000)
        {
            var config = new ListenerConfig("127.0.0.1", GetLocalFreePort(), backlog);
            return config;
        }
    }
}
