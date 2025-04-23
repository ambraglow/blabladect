using System.IO.Ports;
using System.Timers;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace Busmail
{
    public class MessageBus
    {
        SerialPort Serial { get; set; }
        public Buffer SerialBuf = new Buffer();
        public Data busData = new Data();
        public bool Connected = false;
        internal MessageBusOutgoing _busout;
        internal MessageBusIncoming _busin;
        public MessageBus(int baudRate = 115200)
        {
            string[] ports = SerialPort.GetPortNames();
            Serial = new SerialPort(ports[0], baudRate);
            _busout = new MessageBusOutgoing(this);
            _busin = new MessageBusIncoming(this);
        }

        internal Err Init(){
            Serial.Open();
            SerialBuf.Read = new byte[50];
            return Err.Success;
        }

        internal void Read(int length, int offset)
        {
            try {
                Serial.Read(SerialBuf.Read, offset, length);
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
            SerializeFrame(frame);
            busData.SavedFrame = frame;
            // write to _bus
            if(SerialBuf.Write != null)
                Serial.Write(SerialBuf.Write, 0, SerialBuf.Write.Length);
            // handle incoming data on serial _bus
            MessageBusIncoming.HandleFrameIncoming(this);
        }
        public void Clear() {
            if(this.SerialBuf.Read != null)
                Array.Clear(this.SerialBuf.Read, 0, this.SerialBuf.Read.Length);
            Serial.DiscardInBuffer();
        }
        public byte[] SerializeFrame(BusMailFrame frame)
        {
            SerialBuf.Write = new byte[frame.Length + 4];
            SerialBuf.Write[0] = frame.FrameChar;
            SerialBuf.Write[1] = (byte)(frame.Length >> 8);
            SerialBuf.Write[2] = (byte)(frame.Length & 0xFF);
            SerialBuf.Write[3] = frame.Header;
            if(frame.Mail != null) Array.Copy(frame.Mail, 0, SerialBuf.Write, 4, frame.Mail.Length);
            SerialBuf.Write[SerialBuf.Write.Length - 1] = frame.Checksum;

            byte[] message = SerialBuf.Write;

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(BitConverter.ToString(message).Replace("-", " ")+"\n");
            Console.ResetColor();

            return SerialBuf.Write;
        }
    }

    public class MessageBusOutgoing
    {
        private readonly MessageBus _bus;
        internal MessageBusOutgoing(MessageBus bus) {
            _bus = bus;
            Connect();
        }
        public void SyncFrame(bool pf = false) {
                var sabmFrame = FrameBuilder.BuildFrame(FrameType.Unnumbered, null, pf);
                _bus.busData.PollFinal = true;
                _bus.Write(sabmFrame);
        }
        public void InfoFrame(ushort primitive, bool pf = false, byte[] parameters = null) {
            // [1] create mail length bytes and [2] assign size of parameters to the value
            var mailLength = sizeof(ushort);
            if(parameters != null) mailLength += parameters.Length;
            // save pf value of current frame to global PollFinal value
            _bus.busData.PollFinal = pf;
            // build the Mail part of the frame
            byte[] data = new byte[mailLength];
            data[0] = (byte)(primitive & 0xFF);
            data[1] = (byte)(primitive >> 8);
            if(parameters != null) Array.Copy(parameters, 0, data, 2, parameters.Length);

            var informationFrame = FrameBuilder.BuildFrame(FrameType.Information, data, pf);
            _bus.Write(informationFrame);
        }
        public void SupervisoryFrame(bool pf = false) {
            var Supervisory = FrameBuilder.BuildFrame(FrameType.Supervisory, null, pf, SupervisorId.ReceiveReady);
            _bus.busData.PollFinal = pf;
            _bus.Write(Supervisory);
        }
        public void Connect(){
            _bus.Init();

            if(_bus.Connected == false && _bus.busData.Lost != 6){
                Console.Write("Connecting... Sending SABM frame: ");

                SyncFrame(true);

                while(true){
                    if(_bus.Connected == true) {
                        Console.WriteLine("Connected!\n");
                        break;
                    } 
                    else if(_bus.Connected == false) {
                        //MessageBusOutgoing.SyncFrame(_bus, true);
                    }
                }
            }
        }
    }
    public class MessageBusIncoming 
    {
        private readonly MessageBus _bus;
        internal MessageBusIncoming(MessageBus bus) {
            _bus = bus;
        }
        private static System.Timers.Timer _pfTimer = new (interval: 1000);
        internal static void TimerTimeout(Object sender, ElapsedEventArgs e, MessageBus _bus)
        {
            if(_bus.Connected == true && _bus.busData.IncomingPollFinal == false) {
                _bus.busData.PollFinal = false;
                _pfTimer.Stop();
                Debug.WriteLine("Timer stopped.");
            } else {
                Console.Write("Resending frame: ");
                if(_bus.busData.Lost == 6) {
                    _bus.busData.PollFinal = false;
                    _pfTimer.Stop();
                    Debug.WriteLine("Timer elapsed (max lost packets count)");
                } else {
                    _bus.busData.Lost++;
                }
                _bus.Write(_bus.busData.SavedFrame);
            }
        }
        internal void TimerStart() {
            _pfTimer.Elapsed += (sender, e) => TimerTimeout(sender!, e, _bus);
            _pfTimer.Enabled = true;
            _pfTimer.Start();
        }
        internal Err HandleFrameIncoming() {
            if(_bus.busData.PollFinal == true){
                TimerStart();
                Debug.WriteLine("Timer started.");
            }
            Console.ForegroundColor = ConsoleColor.Red;
            while(true){
                // scan for delimiter
                _bus.Read(1, 0);
                // check if the byte read is a frame delimeter
                if(_bus.SerialBuf.Read != null && _bus.SerialBuf.Read[0] == 0x10){
                    _bus.busData.IncomingframeData.FrameChar = _bus.SerialBuf.Read[0];
                    // read 3 more bytes (length, header) at ReadBuf[1]
                    _bus.Read(3, 1);
                    _bus.busData.IncomingframeData.Length = (ushort)_bus.SerialBuf.Read[2];
                    _bus.busData.IncomingframeData.Header = _bus.SerialBuf.Read[3];
                    // length for mail + checksum, we're only reading the upper byte of our length number
                    var length = (int)(_bus.SerialBuf.Read[2] + 1);
                    // mail + checksum + everything else
                    var totallength = length+2;
                    _bus.busData.IncomingframeData.Mail = new byte[_bus.busData.IncomingframeData.Length];                    
                    // check the header type, ( '& (3<<6)' removes useless bits)
                    switch (_bus.SerialBuf.Read[3] & (3<<6)) {
                        case (byte)FrameType.Unnumbered:
                            Console.WriteLine("Type Unnumbered control");
                            _bus.Read(length, 4);
                            //Array.Copy(_bus.SerialBuf.Read, 4, _bus.busData.IncomingframeData.Mail, 0, length-1);
                            _bus.busData.IncomingframeData.Checksum = _bus.SerialBuf.Read[totallength];
                            _bus.Connected = true;
                            break;
                        case (byte)FrameType.Information:
                            Console.WriteLine("Type Information");
                            _bus.Read(length, 4);
                            Array.Copy(_bus.SerialBuf.Read, 4, _bus.busData.IncomingframeData.Mail, 0, length-2);
                            _bus.busData.IncomingframeData.Checksum = _bus.SerialBuf.Read[totallength];
                            break;
                        case (byte)FrameType.Supervisory:
                            Console.WriteLine("Type Supervisory control");
                            _bus.Read(length, 4);
                            Array.Copy(_bus.SerialBuf.Read, 4, _bus.busData.IncomingframeData.Mail, 0, length-2);
                            _bus.busData.IncomingframeData.Checksum = _bus.SerialBuf.Read[totallength];
                            break;
                    }
                    break;
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("header: "+_bus.busData.IncomingframeData.Header.ToString("X")+" ");
            Console.Write("mail: "+BitConverter.ToString(_bus.busData.IncomingframeData.Mail).Replace("-", " "));
            Console.Write(" Checksum: "+_bus.busData.IncomingframeData.Checksum.ToString("X")+"\n");
            Console.ResetColor();
            //IncomingHeader(_bus);
            return Err.Success;
        }
        /*
        public static void IncomingHeader(MessageBus _bus) {
            var header = _bus.busData.IncomingframeData.Header;
            while(true) {
                //lock(_locker) {header = _bus.busData.IncomingframeData.Header;}
                switch (header & (3<<6)) {
                    case (byte)FrameType.Unnumbered:
                        UnnumberedHeader(_bus, header);
                        break;
                    case (byte)FrameType.Information:
                        break;
                    case (byte)FrameType.Supervisory:
                        SupervisoryHeader(_bus, header);
                        break;
                }
                break;
            }
        }
        private static void SupervisoryHeader(MessageBus _bus, int header) {
            const byte ReceiveReady = 0x80 | (int)SupervisorId.ReceiveReady;
            const byte ReceiveNotReady = 0x80 | (int)SupervisorId.ReceiveNotReady;
            const byte Rejected = 0x80 | (int)SupervisorId.Reject;

            byte bitmask = (byte)FrameBuilder.TxSeq;
            header &= ~bitmask;

            switch (header) {
                case ReceiveReady:
                    Console.WriteLine("Frame ReceiveReady");
                    break;
                case ReceiveNotReady:
                    Console.WriteLine("Frame ReceiveNotReady");
                    MessageBusOutgoing.SyncFrame(_bus);
                    break;
                case Rejected:
                    Console.WriteLine("Frame Rejected");
                    break;
            }
        }
        private static void UnnumberedHeader(MessageBus _bus, byte header) {
            switch (header) {
                case (byte)FrameType.Unnumbered | FrameBuilder.PollFinalBit:
                    _bus.Connected = false;
                    Console.WriteLine("Disconnected!");
                    Debug.WriteLine("Disconnected, sending sync frame and restarting timer");
                    MessageBusOutgoing.SyncFrame(_bus, true);
                    break;
                case (byte)FrameType.Unnumbered:
                    Console.WriteLine("Scanned Unnumbered frame type");
                    break;
            }
        }
        */
    }
}
