using System;
using System.Text;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EasySocket.Protocols.Filters;
using EasySocket.Protocols.MsgInfos;

namespace EasySocket.Test
{
    [TestClass]
    public class FixedHeaderMsgFilterTest
    {
        class CustomInfo : IMsgInfo
        {
            public int key {get; private set;} = 0;
            public string data {get; private set;} = string.Empty;
        
            public CustomInfo(int key, string data)
            {
                this.key = key;
                this.data = data;
            }
        }

        class CustomFilter : FixedHeaderMsgFilter
        {
            public CustomFilter()
                : base(8)
            {

            }

            protected override int ParseBodySizeFromHeader(ref ReadOnlySequence<byte> buffer)
            {
                return BitConverter.ToInt32(buffer.Slice(0, 4).FirstSpan);
            }

            protected override IMsgInfo ParseMsgInfo(ref ReadOnlySequence<byte> headerSeq, ref ReadOnlySequence<byte> bodySeq)
            {
                int key = BitConverter.ToInt32(headerSeq.Slice(4, 4).FirstSpan);
                string data = Encoding.UTF8.GetString(bodySeq.FirstSpan);           
                return new CustomInfo(key, data);
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            int testKey = 99;
            string testData = "Is Test Body";

            var buffer = GenerateCustomBuffer(testKey, testData);
            var results = Filter<CustomInfo>(new ReadOnlySequence<byte>(buffer, 0, buffer.Length));

            Assert.AreEqual(results[0].key, testKey);
            Assert.AreEqual(results[0].data, testData);
        }

        private byte[] GenerateCustomBuffer(int key, string body)
        {
            var keyBuffer = BitConverter.GetBytes(key);
            var bodyBuffer = Encoding.UTF8.GetBytes(body);

            // size = (totalSize + key) + body
            var totalBuffer = new byte[4 + keyBuffer.Length + bodyBuffer.Length];
            
            // copy size data
            Buffer.BlockCopy(BitConverter.GetBytes(bodyBuffer.Length), 0, totalBuffer, 0, 4);
            
            // copy key data
            Buffer.BlockCopy(keyBuffer, 0, totalBuffer, 4, keyBuffer.Length);
            
            // copy body data
            Buffer.BlockCopy(bodyBuffer, 0, totalBuffer, 4 + 4, bodyBuffer.Length);

            return totalBuffer;
        }   
  
        CustomFilter filter = new CustomFilter();

        private List<TPacketInfo> Filter<TPacketInfo>(ReadOnlySequence<byte> seq)
            where TPacketInfo : class, IMsgInfo
        {
            var lst = new List<TPacketInfo>();

            var reader = new SequenceReader<byte>(seq);
            
            while (true)
            {
                var requestInfo = filter.Filter(ref reader);
                if (requestInfo == null)
                {
                    break;
                }

                var convertedRequestInfo = requestInfo as TPacketInfo;
                Assert.IsNotNull(convertedRequestInfo);

                lst.Add(convertedRequestInfo);

                if (reader.Consumed == reader.Length)
                    break;
            }


            return lst;
        }
    }
}
