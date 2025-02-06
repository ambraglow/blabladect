using System.IO.Ports;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.VisualBasic;
using System.Timers;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks.Dataflow;

namespace Busmail
{
    public class MessageBus
    {
        public static byte[] ReadBuf;
        public static byte[] WriteBuf;
        public BusMailFrame SavedFrame;
        private int _lost = 0;
        public int Lost {
            get => _lost;
            set {
                if (value >=7 ) _lost = 0;
                else _lost = value;
            }
        }
        public bool MessageSabm = false;
        public bool PollFinal = false;

        SerialPort Serial { get; set; }

        public MessageBus(string portName = "/dev/ttyUSB0", int baudRate = 115200)
        {
            Serial = new SerialPort(portName, baudRate);
        }

        private Err Init(){
            Serial.Open();
            ReadBuf = new byte[50];
            return Err.Success;
        }

        internal void Read(int length, int offset)
        {   
            Serial.Read(ReadBuf, offset, length);
        }
        internal void Write()
        {
            Serial.Write(WriteBuf, 0, WriteBuf.Length);
        }

        public void Clear() {
            Serial.DiscardInBuffer();
        }

        public static void InitializeConnection(MessageBus bus){
            bus.Init();
            if(bus.MessageSabm == false && bus.Lost != 7){
                Console.WriteLine("Connecting...");
                var sabmFrame = FrameBuilder.BuildFrame(FrameType.Unnumbered, null, true);
                bus.SavedFrame = sabmFrame;
                bus.PollFinal = true;
                FrameBuilder.FrameToData(sabmFrame);
                bus.Write();
                MessageBusInconming.HandleFrameIncoming(bus);
                if(bus.MessageSabm == true){
                    Console.WriteLine("Connected!");
                }
            }
        }
    }

    public static class MessageBusOutgoing
    {
        public static void InfoFrame(MessageBus bus, ushort primitive, bool pf = false, byte[] parameters = null) {
            var mailLength = sizeof(ushort);
            bus.PollFinal = pf;
            if(parameters != null) mailLength += parameters.Length;
            byte[] data = new byte[mailLength];
            data[0] = (byte)(primitive & 0xFF);
            data[1] = (byte)(primitive >> 8);
            if(parameters != null) Array.Copy(parameters, 0, data, 2, parameters.Length); 
            var informationFrame = FrameBuilder.BuildFrame(FrameType.Information, data, pf);
            FrameBuilder.FrameToData(informationFrame);
            bus.Write();
            MessageBusInconming.HandleFrameIncoming(bus);
        }
    }
    
    internal static class MessageBusInconming 
    {
        private static System.Timers.Timer _pfTimer = new (interval: 1000);
        private static byte[] frameData;
        
        public static Err HandleFrameIncoming(MessageBus bus) {
            if(bus.PollFinal == true){
                _pfTimer.Elapsed += (source, e) => TimerTimeout(source, e, bus);
                _pfTimer.Enabled = true;
                _pfTimer.Start();
                Console.WriteLine("Timer started.");
            }
            
            while(true){
                bus.Read(1, 0);    // continously read 1 byte from the serial port buffer
                if(MessageBus.ReadBuf[0] == 0x10){  // check if the byte read is a frame delimeter
                    bus.Read(3, 1); // read 3 more bytes (length, header) at ReadBuf[1]
                    int length = MessageBus.ReadBuf[2] + 1;  // length for mail + checksum, we're only reading the upper byte of our length number 
                    Console.WriteLine($"Length read from incoming frame: {length} Header: {MessageBus.ReadBuf[3]}");
                    frameData = new byte[MessageBus.ReadBuf.Length];
                    switch (MessageBus.ReadBuf[3] & (3<<6)) {    // check the header
                        case (byte)FrameType.Unnumbered:
                            Console.WriteLine("Type Unnumbered control");
                            bus.Read(length, 4);
                            Array.Copy(MessageBus.ReadBuf, 0, frameData, 0, MessageBus.ReadBuf.Length);
                            bus.MessageSabm = true;
                            break;
                        case (byte)FrameType.Information:
                            Console.WriteLine("Type Information");
                            bus.Read(length, 4);
                            Array.Copy(MessageBus.ReadBuf, 0, frameData, 0, MessageBus.ReadBuf.Length);
                            break;
                        case (byte)FrameType.Supervisory:
                            Console.WriteLine("Type Supervisory control");
                            bus.Read(length, 4);
                            Array.Copy(MessageBus.ReadBuf, 0, frameData, 0, MessageBus.ReadBuf.Length);
                            break;
                    }
                    break;
                }
            }
            bus.Clear();
            Console.WriteLine("frame read: "+BitConverter.ToString(frameData).Replace("-", " "));
            
            return Err.Success;
        }
        private static void TimerTimeout(Object source, ElapsedEventArgs e, MessageBus bus)
        {
            Console.WriteLine("Timer elapsed");
            if(bus.PollFinal == true){
                bus.Lost++;
                var frameToRetransmit = FrameBuilder.FrameToData(bus.SavedFrame); 
                MessageBus.WriteBuf = frameToRetransmit;
                bus.Write();
            }
            _pfTimer.Stop();
        }
    }
}
