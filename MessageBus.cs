using System.IO.Ports;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.VisualBasic;
using System.Timers;

namespace Busmail
{
    public class MessageBus
    {
        public static byte[] ReadBus;
        public static byte[] WriteBus;
        public BusMailFrame[] SavedFrame;
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
        public static void InitializeConnection(){
            MessageBus mb = new MessageBus();
            mb.Init();
            if(mb.MessageSABM == false && mb.lost != mb.max_outstanding){
                var SABM = FrameBuilder.BuildFrame(FrameType.Unnumbered, null, true);
                FrameBuilder.FrameToData(SABM);
                mb.Write();
                HandleFrameIncoming(mb, true);
            }

        }
        public static Err HandleFrameIncoming(MessageBus mb, bool PollFinal) {
            if(PollFinal == true){
                PFTimer.Elapsed += (source, e) => TimerTimeout(source, e);
                PFTimer.Enabled = true;
                PFTimer.Start();
            }
            
            byte[] FrameData = new byte[10];

            while(PFTimer.Enabled){
                mb.Read(1);
                if(MessageBus.ReadBus[0] == 0x10){
                    FrameData[0] = 0x10;
                    mb.Read(5);
                    Array.Copy(MessageBus.ReadBus, 0, FrameData, 1, MessageBus.ReadBus.Length);
                    break;
                }
                else{
                    
                }
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
