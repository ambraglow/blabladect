using System;
using System.Data.Common;
using System.Net.NetworkInformation;
using System.Threading.Tasks.Dataflow;

namespace Busmail
{
    public static class FrameBuilder
    {
        private static readonly byte PollFinalBit = (1<<3);
        private static uint _txSeq;
        private static uint TxSeq
        {
            get => _txSeq;
            set
            {
                if (value >=7 ) _txSeq = 0;
                else _txSeq = value;
            }
        }

        private static uint RxSeq
        {
            get => TxSeq + 1;
        }

        public static BusMailFrame BuildFrame(FrameType type, byte[] data = null, bool pollFinal = false, SupervisorId Id = SupervisorId.ReceiveNotReady)
        {
            BusMailFrame frame = new BusMailFrame
            {
                FrameChar = 0x10
            };
            switch (type)
            {
                case FrameType.Supervisory:
                    switch (Id)
                    {
                        case SupervisorId.ReceiveNotReady:
                            frame.Header = (byte)((pollFinal ? PollFinalBit : 0x0) | (byte)FrameType.Supervisory | (byte)SupervisorId.ReceiveNotReady | (byte)RxSeq);
                            break;
                        case SupervisorId.ReceiveReady:
                            frame.Header = (byte)((pollFinal ? PollFinalBit : 0x0) | (byte)FrameType.Supervisory | (byte)SupervisorId.ReceiveReady | (byte)RxSeq);
                            break;
                        case SupervisorId.Reject:
                            frame.Header = (byte)((pollFinal ? PollFinalBit : 0x0) | (byte)FrameType.Supervisory | (byte)SupervisorId.Reject | (byte)RxSeq);
                            break;
                    }
                    frame.Length = 0x0001;
                    frame.Checksum = frame.Header;
                    break;
                case FrameType.Information:
                    BuildInformationHeader(ref frame.Header, pollFinal);
                    frame.Mail = new byte[data.Length + 2];
                    Array.Copy(data, 0, frame.Mail, 2, data.Length);
                    frame.Mail[0] = 0x00; // Program ID
                    frame.Mail[1] = 0x01; // Task ID
                    frame.Length = (ushort)(frame.Mail.Length + 1);
                    frame.Checksum = CalculateChecksum(frame);
                    break;
                case FrameType.Unnumbered:
                    frame.Header = (byte)((byte)FrameType.Unnumbered | (pollFinal ? PollFinalBit : 0x0));
                    frame.Length = 0x0001;
                    frame.Checksum = frame.Header;
                    break;
            }
            return frame;
        }

        private static byte CalculateChecksum(BusMailFrame frame) => frame.Mail.Aggregate(frame.Header, (acc, b) => (byte)(acc + b));

        private static void BuildInformationHeader(ref byte header, bool pollFinal)
        {
            //Console.WriteLine($"TxSeq: {TxSeq}, RxSeq: {RxSeq}, PollFinal: {pollFinal}");
            byte txSeqBits = (byte)(TxSeq << 4);
            header = (byte)((pollFinal ? PollFinalBit : 0x0) | txSeqBits | (byte)RxSeq);
            TxSeq++;
        }

        public static byte[] FrameToData(BusMailFrame frame)
        {
            MessageBus.WriteBuf = new byte[frame.Length + 4];
            MessageBus.WriteBuf[0] = frame.FrameChar;
            MessageBus.WriteBuf[1] = (byte)(frame.Length >> 8);
            MessageBus.WriteBuf[2] = (byte)(frame.Length & 0xFF);
            MessageBus.WriteBuf[3] = frame.Header;
            if(frame.Mail != null) Array.Copy(frame.Mail, 0, MessageBus.WriteBuf, 4, frame.Mail.Length);
            MessageBus.WriteBuf[MessageBus.WriteBuf.Length - 1] = frame.Checksum;

            byte[] message = MessageBus.WriteBuf;

            Console.WriteLine("sending frame: "+BitConverter.ToString(message).Replace("-", " "));

            return MessageBus.WriteBuf;
        }
    }
}
