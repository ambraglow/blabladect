using System.IO.Ports;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.VisualBasic;
using System.Timers;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks.Dataflow;
using System.Diagnostics;
using System.Collections;
using System.Data;
using System.Runtime.InteropServices.Swift;
using System.ComponentModel;

namespace Busmail
{
    public class MessageBus
    {
        public static byte[] ReadBuf;
        public static byte[] WriteBuf;
        public BusMailFrame SavedFrame;
        public BusMailFrame IncomingframeData;
        private int _lost = 0;
        public int Lost {
            get => _lost;
            set {
                if (value >=7 ) _lost = 0;
                else _lost = value;
            }
        }
        internal bool Connected = false;
        public bool PollFinal = false;

        SerialPort Serial { get; set; }

        public MessageBus(string portName = "/dev/ttyUSB0", int baudRate = 115200)
        {
            string[] ports = SerialPort.GetPortNames();
            Serial = new SerialPort(ports[0], baudRate);
        }

        private Err Init(){
            Serial.Open();
            ReadBuf = new byte[50];
            return Err.Success;
        }

        internal void Read(int length, int offset)
        {
            try {
                Serial.Read(ReadBuf, offset, length);
            }
            catch (ArgumentException e) 
            {
                throw new ArgumentOutOfRangeException(
                    "blabla", e);
            }
        }
        internal void Write(BusMailFrame frame)
        {
            // serialize
            FrameBuilder.SerializeFrame(frame);
            this.SavedFrame = frame;
            // write to bus
            Serial.Write(WriteBuf, 0, WriteBuf.Length);
            // handle incoming data on serial bus
            MessageBusIncoming.HandleFrameIncoming(this);
        }

        public void Clear() {
            Serial.DiscardInBuffer();
        }

        public static void InitializeConnection(MessageBus bus){
            //Thread t = new Thread(() => MessageBusIncoming.IncomingHeader(bus));
            //t.Name = "Scanning header";
            //t.Start();
            bus.Init();

            if(bus.Connected == false && bus.Lost != 6){
                Console.Write("Connecting... Sending SABM frame: ");

                MessageBusOutgoing.SyncFrame(bus, true);

                while(true){
                    if(bus.Connected == true) {
                        Console.WriteLine("Connected!\n");
                        break;
                    } 
                    else if(bus.Connected == false) {
                        //MessageBusOutgoing.SyncFrame(bus, true);
                    }
                }
            }
        }
    }

    public static class MessageBusOutgoing
    {
        public static void SyncFrame(MessageBus bus, bool pf = false) {
                var sabmFrame = FrameBuilder.BuildFrame(FrameType.Unnumbered, null, pf);
                bus.PollFinal = true;
                bus.Write(sabmFrame);
        }
        public static void InfoFrame(MessageBus bus, ushort primitive, bool pf = false, byte[] parameters = null) {
            // [1] create mail length bytes and [2] assign size of parameters to the value
            var mailLength = sizeof(ushort);
            if(parameters != null) mailLength += parameters.Length;
            // save pf value of current frame to global PollFinal value
            bus.PollFinal = pf;
            // build the Mail part of the frame
            byte[] data = new byte[mailLength];
            data[0] = (byte)(primitive & 0xFF);
            data[1] = (byte)(primitive >> 8);
            if(parameters != null) Array.Copy(parameters, 0, data, 2, parameters.Length);

            var informationFrame = FrameBuilder.BuildFrame(FrameType.Information, data, pf);
            bus.Write(informationFrame);
        }
    }
    internal static class MessageBusIncoming 
    {
        static readonly object _locker = new object();
        private static System.Timers.Timer _pfTimer = new (interval: 1000);
        private static void TimerTimeout(Object sender, ElapsedEventArgs e, MessageBus bus)
        {
            if(bus.Connected == true) {
                bus.PollFinal = false;
                _pfTimer.Stop();
                Debug.WriteLine("Timer stopped.");
            }
            else {
                Console.Write("Resending frame: ");
                if(bus.Lost == 6) {
                    bus.PollFinal = false;
                    _pfTimer.Stop();
                    Debug.WriteLine("Timer elapsed (max lost packets count)");
                } else {
                    bus.Lost++;
                }
                bus.Write(bus.SavedFrame);
            }
        }
        private static void TimerStart(MessageBus bus) {
            _pfTimer.Elapsed += (sender, e) => TimerTimeout(sender!, e, bus);
            _pfTimer.Enabled = true;
            _pfTimer.Start();
        }
        public static Err HandleFrameIncoming(MessageBus bus) {
            if(bus.PollFinal == true){
                TimerStart(bus);
                Debug.WriteLine("Timer started.");
            }
            Console.ForegroundColor = ConsoleColor.Red;
            while(true){
                // scan for delimiter
                bus.Read(1, 0);
                // check if the byte read is a frame delimeter
                if(MessageBus.ReadBuf[0] == 0x10){
                    bus.IncomingframeData.FrameChar = MessageBus.ReadBuf[0];
                    // read 3 more bytes (length, header) at ReadBuf[1]
                    bus.Read(3, 1);

                    bus.IncomingframeData.Length = (ushort)(MessageBus.ReadBuf[2]);
                    bus.IncomingframeData.Header = MessageBus.ReadBuf[3];

                    // length for mail + checksum, we're only reading the upper byte of our length number
                    var length = (int)(MessageBus.ReadBuf[2] + 1);
                    // mail + checksum + everything else
                    var totallength = length+2;
                    
                    bus.IncomingframeData.Mail = new byte[bus.IncomingframeData.Length];

                    //Debug.WriteLine($"Length read from incoming frame: {length} Header: {MessageBus.ReadBuf[3]}");
                    
                    // check the header type, ( '& (3<<6)' removes useless bits)
                    switch (MessageBus.ReadBuf[3] & (3<<6)) {
                        case (byte)FrameType.Unnumbered:
                            Console.WriteLine("Type Unnumbered control");
                            bus.Read(length, 4);
                            //Array.Copy(MessageBus.ReadBuf, 4, bus.IncomingframeData.Mail, 0, length-1);
                            bus.IncomingframeData.Checksum = MessageBus.ReadBuf[totallength];
                            bus.Connected = true;
                            break;
                        case (byte)FrameType.Information:
                            Console.WriteLine("Type Information");
                            bus.Read(length, 4);
                            Array.Copy(MessageBus.ReadBuf, 4, bus.IncomingframeData.Mail, 0, length-1);
                            bus.IncomingframeData.Checksum = MessageBus.ReadBuf[totallength];
                            break;
                        case (byte)FrameType.Supervisory:
                            Console.WriteLine("Type Supervisory control");
                            bus.Read(length, 4);
                            Array.Copy(MessageBus.ReadBuf, 4, bus.IncomingframeData.Mail, 0, length-1);
                            bus.IncomingframeData.Checksum = MessageBus.ReadBuf[totallength];
                            break;
                    }
                    break;
                }
            }
            bus.Clear();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("header: "+bus.IncomingframeData.Header.ToString("X")+" ");
            Console.Write("mail: "+BitConverter.ToString(bus.IncomingframeData.Mail).Replace("-", " "));
            Console.Write(" Checksum: "+bus.IncomingframeData.Checksum.ToString("X")+"\n");
            Console.ResetColor();

            IncomingHeader(bus);
            return Err.Success;
        }
        public static void IncomingHeader(MessageBus bus) {
            var header = bus.IncomingframeData.Header;
            while(true) {
                //lock(_locker) {header = bus.IncomingframeData.Header;}
                switch (header & (3<<6)) {
                    case (byte)FrameType.Unnumbered:
                        UnnumberedHeader(bus, header);
                        break;
                    case (byte)FrameType.Information:
                        break;
                    case (byte)FrameType.Supervisory:
                        SupervisoryHeader(bus, header);
                        break;
                }
                break;
            }
        }
        private static void SupervisoryHeader(MessageBus bus, int header) {
            const byte ReceiveReady = 0x80 | (int)SupervisorId.ReceiveReady;
            const byte ReceiveNotReady = 0x80 | (int)(3 << 4);
            const byte Rejected = 0x80 | (int)(1 << 4);

            byte bitmask = (byte)(FrameBuilder.TxSeq);
            header &= ~(bitmask);

            switch (header) {
                case ReceiveReady:
                    Console.WriteLine("Frame ReceiveReady");
                    break;
                case ReceiveNotReady:
                    Console.WriteLine("Frame ReceiveNotReady");
                    MessageBusOutgoing.SyncFrame(bus);
                    break;
                case Rejected:
                    Console.WriteLine("Frame Rejected");
                    break;
            }
        }
        private static void UnnumberedHeader(MessageBus bus, byte header) {
            switch (header) {
                case (byte)FrameType.Unnumbered | FrameBuilder.PollFinalBit:
                    bus.Connected = false;
                    Console.WriteLine("Disconnected!");
                    Debug.WriteLine("Disconnected, sending sync frame and restarting timer");
                    MessageBusOutgoing.SyncFrame(bus, true);
                    break;
                case (byte)FrameType.Unnumbered:
                    Console.WriteLine("Scanned Unnumbered frame type");
                    break;
            }
        }
    }
}
