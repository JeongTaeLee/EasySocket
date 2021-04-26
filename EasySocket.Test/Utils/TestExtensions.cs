using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using EasySocket.Client;
using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Server;
using EasySocket.Test.Components;
using EasySocket.Common.Protocols.MsgFilters;
using EasySocket.Server.Listeners;

namespace EasySocket.Test
{
    public static class TestExtensions
    {
        public static async Task<TcpStreamPipeSocketServer> StartTcpSocketServer(ListenerConfig lstnCnfg, EventSessionBehaviour ssnBhvr = null)
        {
            var server = new TcpStreamPipeSocketServer()
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(ssnBhvr ?? new EventSessionBehaviour());
                });

            await server.StartAsync(lstnCnfg);

            return server;
        }

        public static async Task<TcpStreamPipeSocketServer> StartTcpSocketServer(EventSessionBehaviour ssnBhvr = null)
        {
            var server = new TcpStreamPipeSocketServer()
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(ssnBhvr ?? new EventSessionBehaviour());
                });

            await server.StartAsync();

            return server;
        }

        public static async Task<TcpStreamPipeSocketServer> StartTcpSocketServer<TMsgFilter>(ListenerConfig lstnCnfg, EventSessionBehaviour ssnBhvr = null)
            where TMsgFilter : class, IMsgFilter, new()
        {
            var server = new TcpStreamPipeSocketServer()
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<TMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(ssnBhvr ?? new EventSessionBehaviour());
                });

            await server.StartAsync(lstnCnfg);

            return server;
        }

        public static async Task<TcpStreamPipeSocketServer> StartTcpSocketServer<TMsgFilter>(EventSessionBehaviour ssnBhvr = null)
            where TMsgFilter : class, IMsgFilter, new()
        {
            var server = new TcpStreamPipeSocketServer()
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<TMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(ssnBhvr ?? new EventSessionBehaviour());
                });

            await server.StartAsync();

            return server;
        }

        public static async Task<TcpStreamPipeSocketClient> ConnectTcpSocketClient(string ip, int port, EventClientBehavior cntBhvr = null)
        {
            var client = new TcpStreamPipeSocketClient()
                .SetLogger(new ConsoleLoggerFactory().GetLogger(typeof(TcpStreamPipeSocketClient)))
                .SetMsgFilter(new StringMsgFilter())
                .SetClientBehaviour(cntBhvr ?? new EventClientBehavior());
            

            await client.StartAsync(ip, port);

            return client;
        }

        public static async Task<List<TcpStreamPipeSocketClient>> ConnectTcpSocketClients(string ip, int port, int connectCount, EventClientBehavior clientBehavior = null)
        {
            var lst = new List<TcpStreamPipeSocketClient>();

            for (int index = 0; index < connectCount; ++index)
            {
                var client = new TcpStreamPipeSocketClient()
                    .SetLogger(new ConsoleLoggerFactory().GetLogger(typeof(TcpStreamPipeSocketClient)))
                    .SetMsgFilter(new StringMsgFilter())
                    .SetClientBehaviour(clientBehavior ?? new EventClientBehavior());

                await client.StartAsync(ip, port);

                lst.Add(client);
            }

            return lst;
        }

        public static async Task<TcpStreamPipeSocketClient> ConnectTcpSocketClient<TMsgFilter>(string ip, int port, EventClientBehavior cntBhvr = null)
           where TMsgFilter : class, IMsgFilter, new()
        {
            var client = new TcpStreamPipeSocketClient()
                .SetLogger(new ConsoleLoggerFactory().GetLogger(typeof(TcpStreamPipeSocketClient)))
                .SetMsgFilter(new TMsgFilter())
                .SetClientBehaviour(cntBhvr ?? new EventClientBehavior());
            
            await client.StartAsync(ip, port);

            return client;
        }

        public static async Task<List<TcpStreamPipeSocketClient>> ConnectTcpSocketClients<TMsgFilter>(string ip, int port, int connectCount, EventClientBehavior clientBehavior = null)
            where TMsgFilter : class, IMsgFilter, new()
        {
            var lst = new List<TcpStreamPipeSocketClient>();

            for (int index = 0; index < connectCount; ++index)
            {
                var client = new TcpStreamPipeSocketClient()
                    .SetLogger(new ConsoleLoggerFactory().GetLogger(typeof(TcpStreamPipeSocketClient)))
                    .SetMsgFilter(new TMsgFilter())
                    .SetClientBehaviour(clientBehavior ?? new EventClientBehavior());

                await client.StartAsync(ip, port);

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