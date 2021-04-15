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
                .AddListener(listenerConfig)
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(sessionBehavior ?? new EventSessionBehaviour());
                });

        }

        public static void ForAll<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var iter in source)
            {
                action(iter);
            }
        }
    }
}