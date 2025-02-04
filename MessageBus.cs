using System.IO.Ports;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.VisualBasic;
using System.Timers;
using System.Reflection.Metadata.Ecma335;

namespace Busmail
{
    public class MessageBus
    {
        public static byte[] ReadBus;
        public static byte[] WriteBus;
        public BusMailFrame SavedFrame;
        public int max_outstanding = 7;
        public int lost = 0;
        public bool MessageSABM = false;
        public bool TimerRunning = true;
        public bool PollFinal;

        SerialPort Serial { get; set; }

        public MessageBus(string portName = "/dev/ttyUSB0", int baudRate = 115200)
        {
            Serial = new SerialPort(portName, baudRate);
        }

        public Err Init(){
            Serial.Open();
            return Err.Success;
        }

        public Err Read(int Length)
        {   
            ReadBus = new byte[Length];
            Serial.Read(ReadBus, 0, Length);
            return Err.Success;
        }
        public Err Write()
        {
            Serial.Write(WriteBus, 0, WriteBus.Length);
            return Err.Success;
        }

    }

    public static class MessageBusHandle 
    {
        public static System.Timers.Timer PFTimer = new (interval: 1000);
        public static void InitializeConnection(MessageBus bus){
            bus.Init();
            if(bus.MessageSABM == false && bus.lost != bus.max_outstanding){
                Console.WriteLine("Connecting...");
                var SABM = FrameBuilder.BuildFrame(FrameType.Unnumbered, null, true);
                bus.SavedFrame = SABM;
                FrameBuilder.FrameToData(SABM);
                bus.Write();
                HandleFrameIncoming(bus, true);
                if(bus.MessageSABM == true){
                    Console.WriteLine("Connected!");
                }
            }
        }
        public static void InfoFrame(MessageBus bus, ushort primitive, bool PollFinal = false, byte[] parameters = null) {
            var MailLength = sizeof(ushort) + parameters.Length;
            byte[] data = new byte[MailLength];
            data[0] = (byte)(primitive >> 8);
            data[1] = (byte)(primitive & 0xFF);
            Array.Copy(parameters, 0, data, 0, data.Length);
            var InformationFrame = FrameBuilder.BuildFrame(FrameType.Information, data, false);
            FrameBuilder.FrameToData(InformationFrame);
            bus.Write();
            HandleFrameIncoming(bus, false);
        }
        public static Err HandleFrameIncoming(MessageBus mb, bool PollFinal) {
            if(PollFinal == true){
                PFTimer.Elapsed += (source, e) => TimerTimeout(source, e);
                PFTimer.Enabled = true;
                PFTimer.Start();
            }
            
            byte[] FrameData = new byte[10];

            while(true){
                mb.Read(1);
                if(MessageBus.ReadBus[0] == 0x10){
                    FrameData[0] = 0x10;
                    mb.Read(5);
                    Array.Copy(MessageBus.ReadBus, 0, FrameData, 1, MessageBus.ReadBus.Length);
                    break;
                }
                else{
                    //retransmit();
                    //increase lost packets count
                }
            }
            if(FrameData[4] == 0xC0){
                mb.MessageSABM = true;
            }

            Console.WriteLine("frame read: "+BitConverter.ToString(FrameData).Replace("-", " "));
            
            return Err.Invalid;
        }
        private static void TimerTimeout(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Timer elapsed");
            PFTimer.Stop();
            PFTimer.Enabled = false;
        }
    }
}
