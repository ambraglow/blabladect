using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Microsoft.VisualBasic;

namespace Busmail
{
    internal struct BusMailFrame
    {
        internal byte FrameChar;
        internal ushort Length;
        internal byte Header;
        internal byte[] mail;
        internal byte Checksum;
    }
   internal struct Mail {
        internal byte programId;
        internal byte TaskId;
        internal ushort Primitive;
        internal byte[] Parameters;
    }
    internal enum FrameType {
        Supervisory,
        Unnumbered,
        Information
    }
    public enum Err
    {
        Success,
        InvalidMessage
    }

    public class Message
    {
        static ReadOnlySpan<byte> FrameData => [0x40, 0x05];
        static internal uint TxSeq = 0;
        static internal uint RxSeq = 0;

        public Err read()
        {
            return readImpl();
        }
        public Err write()
        {
            return writeImpl();
        }
        static byte[] BusMailFrameToData(BusMailFrame frame)
        {
            // Add 4; 3 for the first 3 bytes, 1 for the checksum at the end.
            byte[] data = new byte[frame.Length + 4];
        
            data[0] = frame.FrameChar;
            data[1] = (byte)(frame.Length >> 8);
            data[2] = (byte)(frame.Length & 0xFF);
            //Console.Write();
        
            data[3] = frame.Header;

            Array.Copy(frame.mail, 0, data, 4, frame.mail.Length);
        
            data[data.Length - 1] = frame.Checksum;
        
            return data;
        }
        static BusMailFrame BuildFrame(FrameType type, byte[] data, bool PollFinal) 
        {
            BusMailFrame frame = new BusMailFrame();
            // Frame delimiter
            frame.FrameChar = 0x10;
            switch(type) {
                case FrameType.Supervisory:
                    frame.Header = 0x00;
                    break;
                case FrameType.Information:
                    // build information header
                    InformationHeader(ref frame.Header, ref PollFinal);
                    frame.mail = new byte[data.Length+2];
                    Array.Copy(data, 0, frame.mail, 2, data.Length);
                    // Program Id
                    frame.mail[0] = 0x00;
                    // Task Id
                    frame.mail[1] = 0x01;
                    // Length
                    int size = frame.mail.Length + 1;   // frame length should be equal to mail size + header
                    //Console.WriteLine("Mail size: "+size);
                    frame.Length = (ushort)(size);
                    foreach (byte bytes in frame.mail)
                    {
                        frame.Checksum += bytes;
                    }
                    frame.Checksum += frame.Header;
                    break;
                case FrameType.Unnumbered:
                    // Unnumbered type header with PollFinal bit set
                    frame.Header = 0xC8;
                    frame.Length = 0x01;
                    frame.Checksum = 0xC8;
                    break;
            }
            return frame;
        }
        internal static void InformationHeader(ref byte Header, ref bool PollFinal){
            Console.WriteLine("TxSeq: "+TxSeq+"Pollfinal: "+PollFinal);

            byte TxSeqBits = (byte)(TxSeq);
            TxSeqBits = (byte)(TxSeqBits << 3);

            Console.WriteLine(TxSeqBits.ToString());

            byte RxSeqBits = (byte)(RxSeq);
            
            switch(PollFinal) {
                case true:
                    Header = 0x4;
                    Header = (byte)(Header | TxSeqBits);
                    Header = (byte)(Header | RxSeqBits);
                    break;
                case false:
                    Header = TxSeqBits;
                    Header = (byte)(Header | RxSeqBits);
                    break;
            }

            if(TxSeq == 7)
                TxSeq = 0;
            else
                TxSeq += 1;
        }


        public static void Main() {
            //var SABMFrame = Message.BuildFrame(FrameType.Unnumbered, null);
            //byte[] reconstructedone = Message.BusMailFrameToData(SABMFrame);

            var InfoFrame = Message.BuildFrame(FrameType.Information, FrameData.ToArray(), true);
            byte[] reconstructedtwo = Message.BusMailFrameToData(InfoFrame);

            var InfoFrametwo = Message.BuildFrame(FrameType.Information, FrameData.ToArray(), true);
            byte[] reconstructedThree = Message.BusMailFrameToData(InfoFrametwo);

            //Console.WriteLine(BitConverter.ToString(reconstructedone).Replace("-", ""));
            Console.WriteLine(BitConverter.ToString(reconstructedtwo).Replace("-", " "));
            Console.WriteLine(BitConverter.ToString(reconstructedThree).Replace("-", " "));
        }

        protected virtual Err readImpl() => 0;
        protected virtual Err writeImpl() => 0;
    }
}