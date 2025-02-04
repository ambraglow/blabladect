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
        public static byte[] ReadBus;
        public static byte[] WriteBus;
        public BusMailFrame SavedFrame;
        public int _lost = 0;
        public int lost {
            get => _lost;
            set {
                if (value >=7 ) _lost = 0;
                else _lost = value;
            }
        }
        public bool MessageSABM = false;
        public bool PollFinal = false;

        SerialPort Serial { get; set; }

        public MessageBus(string portName = "/dev/ttyUSB0", int baudRate = 115200)
        {
            Serial = new SerialPort(portName, baudRate);
        }

        public Err Init(){
            Serial.Open();
            ReadBus = new byte[50];
            return Err.Success;
        }

        public void Read(int Length, int offset)
        {   
            Serial.Read(ReadBus, offset, Length);
        }
        public void Write()
        {
            Serial.Write(WriteBus, 0, WriteBus.Length);
        }

        public void Clear() {
            Serial.DiscardInBuffer();
        }

    }

    public static class MessageBusHandle 
    {
        public static System.Timers.Timer PFTimer = new (interval: 1000);
        public static byte[] FrameData;
        public static void InitializeConnection(MessageBus bus){
            bus.Init();
            if(bus.MessageSABM == false && bus.lost != 7){
                Console.WriteLine("Connecting...");
                var SABM = FrameBuilder.BuildFrame(FrameType.Unnumbered, null, true);
                bus.SavedFrame = SABM;
                bus.PollFinal = true;
                FrameBuilder.FrameToData(SABM);
                bus.Write();
                HandleFrameIncoming(bus);
                if(bus.MessageSABM == true){
                    Console.WriteLine("Connected!");
                }
            }
        }
        public static void InfoFrame(MessageBus bus, ushort primitive, bool pf = false, byte[] parameters = null) {
            var MailLength = sizeof(ushort);
            bus.PollFinal = pf;
            if(parameters == null) { 
;           }
            else{
                MailLength += parameters.Length;
            }
            byte[] data = new byte[MailLength];
            data[0] = (byte)(primitive & 0xFF);
            data[1] = (byte)(primitive >> 8);
            if(parameters == null) { }
            else{
                Array.Copy(parameters, 0, data, 2, parameters.Length);
            }
            var InformationFrame = FrameBuilder.BuildFrame(FrameType.Information, data, pf);
            FrameBuilder.FrameToData(InformationFrame);
            bus.Write();
            HandleFrameIncoming(bus);
        }
        public static Err HandleFrameIncoming(MessageBus bus) {
            if(bus.PollFinal == true){
                PFTimer.Elapsed += (source, e) => TimerTimeout(source, e, bus);
                PFTimer.Enabled = true;
                PFTimer.Start();
                Console.WriteLine("Timer started.");
            }
            
            while(true){
                bus.Read(1, 0);    // continously read 1 byte from the serial port buffer
                if(MessageBus.ReadBus[0] == 0x10){  // check if the byte read is a frame delimeter
                    bus.Read(3, 1); // read 3 more bytes (length, header) at ReadBus[1]
                    int Length = MessageBus.ReadBus[2] + 1;  // length for mail + checksum, we're only reading the upper byte of our length number 
                    Console.WriteLine($"Length read from incoming frame: {Length} Header: {MessageBus.ReadBus[3]}");
                    FrameData = new byte[MessageBus.ReadBus.Length];
                    switch (MessageBus.ReadBus[3] & (3<<6)) {    // check the header
                        case (byte)FrameType.Unnumbered:
                            Console.WriteLine("Type Unnumbered control");
                            bus.Read(Length, 4);
                            Array.Copy(MessageBus.ReadBus, 0, FrameData, 0, MessageBus.ReadBus.Length);
                            bus.MessageSABM = true;
                            break;
                        case (byte)FrameType.Information:
                            Console.WriteLine("Type Information");
                            bus.Read(Length, 4);
                            Array.Copy(MessageBus.ReadBus, 0, FrameData, 0, MessageBus.ReadBus.Length);
                            break;
                        case (byte)FrameType.Supervisory:
                            Console.WriteLine("Type Supervisory control");
                            bus.Read(Length, 4);
                            Array.Copy(MessageBus.ReadBus, 0, FrameData, 0, MessageBus.ReadBus.Length);
                            //HandleFrameIncoming(bus);
                            break;
                    }
                    break;
                }
            }
            bus.Clear();
            Console.WriteLine("frame read: "+BitConverter.ToString(FrameData).Replace("-", " "));
            
            return Err.Success;
        }
        private static void TimerTimeout(Object source, ElapsedEventArgs e, MessageBus bus)
        {
            Console.WriteLine("Timer elapsed");
            if(bus.PollFinal == true){
                bus.lost++;
                var FrameRetransmit = FrameBuilder.FrameToData(bus.SavedFrame); 
                MessageBus.WriteBus = FrameRetransmit;
                bus.Write();
            }
            PFTimer.Stop();
        }
    }
}
