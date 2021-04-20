using EasySocket.Client;
using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Server;
using EasySocket.Server.Listeners;
using EasySocket.Test.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EasySocket.Test
{
    public static class TestExtensions
    {
        public static TcpSocketServer CreateStringTcpServer(EventSessionBehaviour sessionBehavior = null)
        {
            return new TcpSocketServer()
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(sessionBehavior ?? new EventSessionBehaviour());
                });
        }

        public static async Task<List<TcpSocketClient>> ConnectTcpSocketClient(string ip, int port, int connectCount, EventClientBehavior clientBehavior = null)
        {
            var lst = new List<TcpSocketClient>();

            for (int index = 0; index < connectCount; ++index)
            {
                var client = new TcpSocketClient()
                    .SetLoggerFactory(new ConsoleLoggerFactory())
                    .SetMsgFilter(new StringMsgFilter())
                    .SetSocketClientConfig(new SocketClientConfig(ip, port));
                //.SetClientBehaviour(clientBehavior);

                if (clientBehavior != null)
                {
                    client.SetClientBehaviour(clientBehavior);
                }

                await client.StartAsync();

                lst.Add(client);
            }

            return lst;
        }

        // TODO - ��ġ ���� �ȵ�� �ű⼼��
        public static void ForAll<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var iter in source)
            {
                action(iter);
            }
        }

        // TODO - ��ġ ���� �ȵ�� �ű⼼��
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
    }
}