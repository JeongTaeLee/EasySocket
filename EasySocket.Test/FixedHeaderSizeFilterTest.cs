using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EasySocket.Server;
using EasySocket.Server.Listeners;
using EasySocket.Client;
using EasySocket.Common.Protocols;
using EasySocket.Common.Protocols.Factories;
using EasySocket.Test.Components;

namespace EasySocket.Test
{
    public class TestFixedHeaderSizeFilter : FixedHeaderSizeMsgFilter
    {
        public TestFixedHeaderSizeFilter()
            : base(4)
        {
            
        }

        protected override int ParseBodySizeFromHeader(ref ReadOnlySequence<byte> headerSeq)
        {
            return BitConverter.ToInt32(headerSeq.FirstSpan);
        }

        protected override object ParseMsgInfo(ref ReadOnlySequence<byte> totalSeq)
        {
            return Encoding.Default.GetString(totalSeq.Slice(headerSize));
        }
    }


    [TestClass]
    public class FixedHeaderSizeFilterTest
    {
        public FixedHeaderSizeFilterTest()
        {
        }

        [TestMethod]
        public async Task ServerTest()
        {   
            int port = 9199;
            int serverOnErrorCount = 0;
            var receiveStrs = new List<string>();

            var ssnBhvr = new EventSessionBehaviour();
            ssnBhvr.onReceived += (ssn, packet) => { receiveStrs.Add(packet.ToString()); };

            //
            var server = new TcpStreamPipeSocketServer()
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<TestFixedHeaderSizeFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(ssnBhvr);
                })
                .SetOnError((ssn, ex) =>
                {
                    ++serverOnErrorCount;
                });

            // 서버 시작
            await server.StartAsync(new ListenerConfig("127.0.0.1", port, 1000));

            //
            var client = await TestExtensions.ConnectTcpSocketClient("127.0.0.1", port);
            await Task.Delay(100);

            Assert.AreNotEqual(null, server);
            Assert.AreNotEqual(null, client);

            await BasicTest(receiveStrs, async (bt) => { return await client.SendAsync(bt);});

            await client.StopAsync();
            await server.StopAsync();

            // 서버 에러 확인
            Assert.AreEqual(0, serverOnErrorCount);
        }

        [TestMethod]
        public async Task ClientTest()
        {
            int port = 9199;
            int serverOnErrorCount = 0;

            var session = default(ISession);
            var ssnBhvr = new EventSessionBehaviour();
            ssnBhvr.onStartAfter += (ssn) => { session = ssn; };
            
            var receiveStrs = new List<string>();
            var clntBhvr = new EventClientBehavior();
            clntBhvr.onReceived += (clnt, packet) => { receiveStrs.Add(packet.ToString()); }; 

            //
            var server = new TcpStreamPipeSocketServer()
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<TestFixedHeaderSizeFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(ssnBhvr);
                })
                .SetOnError((ssn, ex) =>
                {
                    ++serverOnErrorCount;
                });
            
            // 서버 시작
            await server.StartAsync(new ListenerConfig("127.0.0.1", port, 1000));

            //
            var client = new TcpStreamPipeSocketClient()
                .SetLogger(new ConsoleLoggerFactory().GetLogger(typeof(TcpStreamPipeSocketClient)))
                .SetMsgFilter(new TestFixedHeaderSizeFilter())
                .SetClientBehaviour(clntBhvr);

            // 클라이언트 시작            
            await client.StartAsync("127.0.0.1", port);
            await Task.Delay(100);
    
            Assert.AreNotEqual(null, server);
            Assert.AreNotEqual(null, client);
            Assert.AreNotEqual(null, session);

            // 비동기 전송
            await BasicTest(receiveStrs, async (bt) => { return await session.SendAsync(bt); });

            // 동기 전송
            await BasicTest(receiveStrs, (bt) => { return ValueTask.FromResult(session.Send(bt)); });

            await client.StopAsync();
            await server.StopAsync();

            // 서버 에러 확인
            Assert.AreEqual(0, serverOnErrorCount);
        }

        private async Task BasicTest(List<string> receiveStrs, Func<byte[], ValueTask<int>> sendFunc)
        {
            receiveStrs.Clear();

            // 기본 송신 테스트.
            {
                var sendStr = "Hello World";
                var sendBt = CreateSendBuffer(sendStr);

                // 전송
                await sendFunc(sendBt);
                await Task.Delay(100);
            
                // 확인
                Assert.AreEqual(1, receiveStrs.Count);
                Assert.AreEqual(sendStr, receiveStrs.First());
            
                receiveStrs.Clear();
            }

            // 두개가 합쳐갔을 때.
            {
                var firstSendStr = "First Send Test";
                var firstSendBt = CreateSendBuffer(firstSendStr);

                var secondSendStr = "Second Send Test";
                var secondSendBt = CreateSendBuffer(secondSendStr);

                var sendBt = new byte[firstSendBt.Length + secondSendBt.Length];
                Buffer.BlockCopy(firstSendBt, 0, sendBt, 0, firstSendBt.Length);
                Buffer.BlockCopy(secondSendBt, 0, sendBt, firstSendBt.Length, secondSendBt.Length);

                int sendSize = await sendFunc(sendBt);
                await Task.Delay(100);

                Assert.AreEqual(2, receiveStrs.Count);
                Assert.AreEqual(firstSendStr, receiveStrs[0]);
                Assert.AreEqual(secondSendStr, receiveStrs[1]);
            
                receiveStrs.Clear();
            }

            // 바디가 끊어서 전달되었을 때
            {
                var sendStr = "Body Split Test";
                var sendBt = CreateSendBuffer(sendStr);
                var sequence = new ReadOnlySequence<byte>(sendBt);

                // 분리한 첫번째 전송.
                int sendSize = await sendFunc(sequence.FirstSpan.Slice(0, 6).ToArray());
                sequence = sequence.Slice(sendSize);
                await Task.Delay(100);
                
                // 확인
                Assert.AreEqual(0, receiveStrs.Count);
            
                // 나머지 데이터 전송.
                sendSize += await sendFunc(sequence.ToArray());
                await Task.Delay(100);

                // 확인.
                Assert.AreEqual(1, receiveStrs.Count);
                Assert.AreEqual(sendStr, receiveStrs.First());
                
                Assert.AreEqual(sendBt.Length, sendSize);

                receiveStrs.Clear();
            }

            // 헤더가 분리되어 전달되었을 때
            {
                var sendStr = "Header Split Test";
                var sendBt = CreateSendBuffer(sendStr);
                var sequence = new ReadOnlySequence<byte>(sendBt);

                // 헤더 분리 첫번째 전송.
                int sendSize = await sendFunc(sequence.FirstSpan.Slice(0, 2).ToArray());
                sequence = sequence.Slice(sendSize);
                await Task.Delay(100);

                // 확인
                Assert.AreEqual(0, receiveStrs.Count);

                // 나머지 전송
                sendSize += await sendFunc(sequence.ToArray());
                await Task.Delay(100);
                
                // 확인
                Assert.AreEqual(1, receiveStrs.Count);
                Assert.AreEqual(sendStr, receiveStrs.First());

                Assert.AreEqual(sendBt.Length, sendSize);

                receiveStrs.Clear();
            }
            
            // 두개가 중간에 분리될 때(첫번째 패킷 + 두번째 패킷 일부 전송, 남은 두번째 패킷 전송. )
            {
                var firstSendStr = "First Send Test";
                var firstSendBt = CreateSendBuffer(firstSendStr);

                var secondSendStr = "Second Send Test";
                var secondSendBt = CreateSendBuffer(secondSendStr);

                var sendBt = new byte[firstSendBt.Length + secondSendBt.Length];
                Buffer.BlockCopy(firstSendBt, 0, sendBt, 0, firstSendBt.Length);
                Buffer.BlockCopy(secondSendBt, 0, sendBt, firstSendBt.Length, secondSendBt.Length);
                
                var sequence = new ReadOnlySequence<byte>(sendBt);

                // 첫번째 전송
                int splitSize = firstSendBt.Length + secondSendBt.Length / 2;
                int sendSize = await sendFunc(sequence.Slice(0, splitSize).ToArray());
                sequence = sequence.Slice(splitSize);
                await Task.Delay(100);

                // 확인
                Assert.AreEqual(1, receiveStrs.Count);
                Assert.AreEqual(firstSendStr, receiveStrs.First());
                
                // 나머지 전송 시작!
                sendSize += await sendFunc(sequence.ToArray());
                await Task.Delay(100);
                
                //확인
                Assert.AreEqual(2, receiveStrs.Count);
                Assert.AreEqual(secondSendStr, receiveStrs[1]);

                Assert.AreEqual(sendBt.Length, sendSize);

                receiveStrs.Clear();
            }

            // 두개가 중간에 분리될 때(첫번째 패킷 일부 전송, 남은 첫번째 패킷 전송 + 두번째 패킷 전송)
            {
                var firstSendStr = "First Send Test";
                var firstSendBt = CreateSendBuffer(firstSendStr);

                var secondSendStr = "Second Send Test";
                var secondSendBt = CreateSendBuffer(secondSendStr);

                var sendBt = new byte[firstSendBt.Length + secondSendBt.Length];
                Buffer.BlockCopy(firstSendBt, 0, sendBt, 0, firstSendBt.Length);
                Buffer.BlockCopy(secondSendBt, 0, sendBt, firstSendBt.Length, secondSendBt.Length);
                
                var sequence = new ReadOnlySequence<byte>(sendBt);

                // 첫번째 전송
                int splitSize = firstSendBt.Length / 2;
                int sendSize = await sendFunc(sequence.Slice(0, splitSize).ToArray());
                sequence = sequence.Slice(splitSize);
                await Task.Delay(100);

                // 확인
                Assert.AreEqual(0, receiveStrs.Count);
                
                // 나머지 전송 시작!
                sendSize += await sendFunc(sequence.ToArray());
                await Task.Delay(100);
                
                //확인
                Assert.AreEqual(2, receiveStrs.Count);
                Assert.AreEqual(firstSendStr, receiveStrs[0]);
                Assert.AreEqual(secondSendStr, receiveStrs[1]);

                Assert.AreEqual(sendBt.Length, sendSize);

                receiveStrs.Clear();
            }
        }

        public byte[] CreateSendBuffer(string msg)
        {
            var bt = new byte[4 + msg.Length];
            var msgBt = Encoding.Default.GetBytes(msg);

            Buffer.BlockCopy(BitConverter.GetBytes(msg.Length), 0, bt, 0, 4);
            Buffer.BlockCopy(msgBt, 0, bt, 4, msgBt.Length);

            return bt;
        }
    }
}