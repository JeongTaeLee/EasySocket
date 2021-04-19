using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Server;
using EasySocket.Server.Listeners;
using EasySocket.Test.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace EasySocket.Test
{
    public static class TestExtensions
    {
        // public static TServer CreateSocketServer<TServer, TSession>(ListenerConfig config, StringServerBehavior srvBhvr = null, StringSessionBehavior ssnBhvr = null)
        //     where TServer : SocketServer<TServer, TSession, string>, new()
        //     where TSession : SocketSession<TSession, string>
        // {
        //     return new TServer()
        //             .AddListener(config)
        //             .SetLoggerFactory(new ConsoleLoggerFactory())
        //             .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter, string>())
        //             .SetServerBehavior(srvBhvr ?? new StringServerBehavior())
        //             .SetSessionConfigrator((ssn) =>
        //             {
        //                 if (ssnBhvr != null)
        //                 {
        //                     ssn.SetSessionBehavior(ssnBhvr);
        //                 }
        //             });
        // }

        public static TcpSocketServer CreateStringTcpServer(ListenerConfig listenerConfig, EventSessionBehaviour sessionBehavior = null)
        {
            return new TcpSocketServer()
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(sessionBehavior ?? new EventSessionBehaviour());
                });
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