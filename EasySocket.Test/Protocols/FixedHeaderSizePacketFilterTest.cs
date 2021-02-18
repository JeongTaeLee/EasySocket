using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EasySocket.Protocols.PacketInfos;
using EasySocket.Protocols.Filters;
using System.Buffers;
using System;
using System.Text;

namespace EasySocket.Test
{
    [TestClass]
    public class FixedHeaderSizePacketFilterTest
    {
        class CustomInfo : IPacketInfo
        {
            int key = 0;
            string data = string.Empty;
        
            public CustomInfo(int key, string data)
            {
                this.key = key;
                this.data = data;
            }
        }

        class CustomFilter : FixedHeaderSizePacketFilter
        {
            public CustomFilter()
                : base(8)
            {

            }

            protected override int ParseBodySizeFromHeader(ref ReadOnlySequence<byte> buffer)
            {
                return BitConverter.ToInt32(buffer.Slice(0, 4).FirstSpan);
            }

            protected override IPacketInfo ParsePacketInfoFromTotal(ref ReadOnlySequence<byte> buffer)
            {
                int key = BitConverter.ToInt32(buffer.Slice(4, 4).FirstSpan);
                string data = Encoding.UTF8.GetString(buffer.Slice(headerSize, buffer.Length - headerSize).FirstSpan);
                return new CustomInfo(key, data);
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            var arrayPool = ArrayPool<byte>.Create();

            var buffer = arrayPool.Rent(1024);

            var keyBuffer = BitConverter.GetBytes(1);
            Buffer.BlockCopy(keyBuffer, 0, buffer, 4, keyBuffer.Length);

            var bodySize = Encoding.UTF8.GetBytes("Type 1 Packet");
            Buffer.BlockCopy(bodySize, 0, buffer, 4 + sizeof(int), bodySize.Length);

            var sizeBuffer = BitConverter.GetBytes(bodySize.Length);
            Buffer.BlockCopy(sizeBuffer, 0, buffer, 0, sizeBuffer.Length);

            Filter(new ReadOnlySequence<byte>(buffer, 0, sizeBuffer.Length + keyBuffer.Length + bodySize.Length));
        }

        CustomFilter filter = new CustomFilter();

        private List<IPacketInfo> Filter(ReadOnlySequence<byte> seq)
        {
            var lst = new List<IPacketInfo>();

            var reader = new SequenceReader<byte>(seq);
            
            while (true)
            {
                var requestInfo = filter.Filter(reader);
                if (requestInfo == null)
                {
                    break;
                }

                lst.Add(requestInfo);

                if (reader.Consumed == reader.Length)
                    break;
            }


            return lst;
        }
    }
}
