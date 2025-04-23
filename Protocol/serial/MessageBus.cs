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

        internal MessageBusOutgoing BusOut;
        internal MessageBusIncoming BusIn;
        
        internal MessageBus(int baudRate = 115200)
        {
            string[] ports = SerialPort.GetPortNames();
            Serial = new SerialPort(ports[0], baudRate);
            BusOut = new MessageBusOutgoing(this);
            BusIn = new MessageBusIncoming(this);
            Init();
            BusOut.Connect();
        }

        private void Init(){
            Serial.Open();
            SerialBuf.Read = new byte[50];
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
            Thread.Sleep(50);   // dio santo che sei nei cieli sia santificato il tuo nome, sia in cielo sia in terra. dacci oggi il nostro pane quotidiano..
            BusIn.HandleFrameIncoming();
        }
        internal void Clear() {
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

    internal class MessageBusOutgoing
    {
        private readonly MessageBus _bus;
        internal MessageBusOutgoing(MessageBus bus) {
            _bus = bus;
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
    internal class MessageBusIncoming 
    {
        private readonly MessageBus _bus;
        internal MessageBusIncoming(MessageBus bus) {
            _bus = bus;
        }
        private static System.Timers.Timer _pfTimer = new (interval: 1000);
        private void TimerTimeout(Object sender, ElapsedEventArgs e)
        {
            if(_bus.Connected == true /*&& _bus.busData.IncomingPollFinal == false*/) {
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
        private void TimerStart() {
            _pfTimer.Elapsed += (sender, e) => TimerTimeout(sender!, e);
            _pfTimer.Enabled = true;
            _pfTimer.Start();
        }
        internal void HandleFrameIncoming() {
            if(_bus.busData.PollFinal == true){
                TimerStart();
                Debug.WriteLine("Timer started.");
            }
            while(true){
                // scan for delimiter
                _bus.Read(1, 0);
                // check if the byte read is a frame delimeter
                if(_bus.SerialBuf.Read[0] == 0x10){
                    _bus.busData.IncomingframeData.FrameChar = _bus.SerialBuf.Read[0];
                    // read 3 more bytes (length, header) at ReadBuf[1]
                    _bus.Read(3, 1);
                    _bus.busData.IncomingframeData.Length = _bus.SerialBuf.Read[2];
                    _bus.busData.IncomingframeData.Header = _bus.SerialBuf.Read[3];
                    // length for mail + checksum, we're only reading the upper byte of our length number
                    var length = _bus.SerialBuf.Read[2] + 1;
                    // mail + checksum + everything else
                    var totallength = length+2;
                    _bus.busData.IncomingframeData.Mail = new byte[_bus.busData.IncomingframeData.Length];           
                    // check the header type, ( '& (3<<6)' removes useless bits)
                    switch (_bus.SerialBuf.Read[3] & (3<<6)) {
                        case (byte)FrameType.Unnumbered:
                            Console.Write("Type Unnumbered ");
                            UnnumberedHeader(length, totallength);
                            break;
                        case (byte)FrameType.Information:
                            Console.Write("Type Information ");
                            InfoHeader(length, totallength);
                            break;
                        case (byte)FrameType.Supervisory:
                            Console.Write("Type Supervisory ");
                            SupervisoryHeader(length, totallength);
                            break;
                    }
                    break;
                }
            }
            _bus.Clear();

            Console.Write("header: "+_bus.busData.IncomingframeData.Header.ToString("X")+" ");
            Console.Write("mail: "+BitConverter.ToString(_bus.busData.IncomingframeData.Mail).Replace("-", " "));
            Console.Write(" Checksum: "+_bus.busData.IncomingframeData.Checksum.ToString("X")+"\n");
            Console.ResetColor();
            //IncomingHeader(_bus);
        }
        private void SupervisoryHeader(int length, int lengthtotal) {
            _bus.Read(length, 4);
            Array.Copy(_bus.SerialBuf.Read, 4, _bus.busData.IncomingframeData.Mail, 0, length-2);
            _bus.busData.IncomingframeData.Checksum = _bus.SerialBuf.Read[lengthtotal];
            /*
            if (_bus.busData.IncomingframeData.Header == (FrameBuilder.PollFinalBit & ~(byte)FrameType.Supervisory)) {
                //_bus.Connected = false;
                Console.WriteLine("inc pf!");
                _bus.BusOut.SupervisoryFrame();
            }
            */
        }
        private void InfoHeader(int length, int lengthtotal) {
            _bus.Read(length, 4);
            Array.Copy(_bus.SerialBuf.Read, 4, _bus.busData.IncomingframeData.Mail, 0, length-2);
            _bus.busData.IncomingframeData.Checksum = _bus.SerialBuf.Read[lengthtotal];
            /*
            if (_bus.busData.IncomingframeData.Header != (FrameBuilder.PollFinalBit & ~(byte)FrameType.Information)) {
                //_bus.Connected = false;
                Console.WriteLine("inc pf!");
                _bus.BusOut.SupervisoryFrame();
            }
            */
        }

        private void UnnumberedHeader(int length, int lengthtotal) {
            _bus.Read(length, 4);
            _bus.busData.IncomingframeData.Checksum = _bus.SerialBuf.Read[lengthtotal];
            if(_bus.busData.IncomingframeData.Header == 0xc0) {
                _bus.Connected = true;
            } 
            /*
            switch (_bus.busData.IncomingframeData.Header) {
                case FrameBuilder.PollFinalBit & ~(byte)FrameType.Unnumbered:
                    _bus.Connected = false;
                    Console.WriteLine("Disconnected!");
                    _bus.BusOut.Connect();
                    break;
                case (byte)FrameType.Unnumbered:
                    _bus.Connected = true;
                    //Console.WriteLine("Scanned Unnumbered frame type");
                    break;
            }
           */ 
        }
    }
}
