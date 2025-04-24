using System.IO.Ports;
using System.Timers;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Net.Mail;
using System.Runtime.CompilerServices;

namespace Busmail
{
    public class MessageBus
    {
        internal SerialPort Serial { get; set; }
        public Buffer SerialBuf = new Buffer();
        public Data busData = new Data();
        public bool Connected = false;
        public bool FrameIncomplete = false;
        internal System.Timers.Timer _retxTimer = new (interval: 1000);
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
            Serial.Handshake = Handshake.None;
            Serial.DataReceived += new SerialDataReceivedEventHandler(HandleDataReceived);
            Serial.Open();
        }
        private void HandleDataReceived(object sender, SerialDataReceivedEventArgs e) {
            if(FrameIncomplete == true)
                BusOut.Retransmit();
            SerialPort sp = (SerialPort)sender;
            int length = sp.BytesToRead;
            if(length > 2){
                SerialBuf.Read = new byte[length];
                Read(length, 0);
                //Console.WriteLine("content read: "+BitConverter.ToString(SerialBuf.Read));
                switch(BusIn.HandleSerialBuffer())
                {
                    case Err.Success:
                        FrameIncomplete = false;
                        break;
                    case Err.Invalid:
                        FrameIncomplete = true;
                        break;
                }
                Clear();
            }
        }
        internal void Read(int length, int offset)
        {
            try {
                Serial.Read(SerialBuf.Read, offset, length);
            }
            catch (ArgumentException e) 
            {
                throw new ArgumentOutOfRangeException(
                    "whoops", e);
            }
        }
        internal void Write(BusMailFrame frame)
        {
            // serialize
            SerializeFrame(frame);
            busData.SavedFrame = frame;
            // write to bus
            if(SerialBuf.Write != null)
                Serial.Write(SerialBuf.Write, 0, SerialBuf.Write.Length);
            // handle incoming data on serial _bus
            Thread.Sleep(50);   // dio santo che sei nei cieli sia santificato il tuo nome, sia in cielo sia in terra. dacci oggi il nostro pane quotidiano..
        }
        internal void Clear() {
            SerialBuf.Read = [];
            Serial.DiscardInBuffer();
        }
        public byte[] SerializeFrame(BusMailFrame frame)
        {
            SerialBuf.Write = new byte[frame.Length + 4];
            SerialBuf.Write[0] = (byte)BusMailFrame.FrameChar;
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
        public void SupervisoryFrame(SupervisorId type, bool pf = false) {
            var Supervisory = FrameBuilder.BuildFrame(FrameType.Supervisory, null, pf, type);
            _bus.busData.PollFinal = pf;
            _bus.Write(Supervisory);
        }
        /*
        internal void TimerStart() {
            Debug.WriteLine("Attemtpting to retransmit frames, starting timer");
            _bus._retxTimer.Elapsed += (sender, e) => _bus.BusOut.Retransmit(sender!, e);
            _bus._retxTimer.Enabled = true;
            _bus._retxTimer.Start();
        }
        */
        internal void Retransmit() {
            if(_bus.busData.Lost == 6) {
                _bus.busData.PollFinal = false;
                _bus._retxTimer.Stop();
                Debug.WriteLine("Timer elapsed (max lost packets count)");
            } else {
                    _bus.busData.Lost++;
            }
            Debug.Write("Retransmitting frame: ");
            _bus.Write(_bus.busData.SavedFrame);
            Thread.Sleep(1000);
        }
        /*
        private void Retransmit(Object sender, ElapsedEventArgs e)
        {
            if(_bus.Connected == true) {
                if(_bus.busData.Lost == 6) {
                    _bus.busData.PollFinal = false;
                    _bus._retxTimer.Stop();
                    Debug.WriteLine("Timer elapsed (max lost packets count)");
                } else {
                    _bus.busData.Lost++;
                }
                _bus.Write(_bus.busData.SavedFrame);
            }
            else {
                if(_bus.busData.Lost == 6) {
                    _bus.busData.PollFinal = false;
                    _bus._retxTimer.Stop();
                    Debug.WriteLine("Timer elapsed (max lost packets count)");
                } else {
                    _bus.busData.Lost++;
                }
                SyncFrame(true);
            }
        }
        */
        public void Connect(){
            if(_bus.Connected == false){
                Console.Write("Connecting... Sending SABM frame: ");
                SyncFrame(true);
                while(true){
                    if(_bus.Connected == true) {
                        Console.WriteLine("Connected!\n");
                        break;
                    } 
                    else if(_bus.Connected == false) {
                        Retransmit();
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
        private static byte ExtractHeaderType(byte value) => (byte)(value & (3 << 6));
        private static T ExtractFromQuery<T>(IEnumerable<T> query) {
            foreach(var result in query){
                return result;
            }
            return default(T);
        }
        internal Err HandleSerialBuffer() {
            // find delimiter first, do the rest if it's present
            var QueryFrameChar = _bus.SerialBuf.Read.OfType<byte>().Where(delimiter => delimiter == BusMailFrame.FrameChar);
            var delimiter = ExtractFromQuery<byte>(QueryFrameChar);
            if(delimiter != 0) {
                var length = _bus.SerialBuf.Read.OfType<byte>().SkipWhile(length => length != delimiter).ElementAt(2);
                _bus.busData.IncomingframeData.Length = (ushort)(length-1);

                var HeaderQuery = _bus.SerialBuf.Read.OfType<byte>()
                    .Where(header => ExtractHeaderType(header) == (byte)FrameType.Unnumbered  
                                || ExtractHeaderType(header) == (byte)FrameType.Supervisory 
                                || ExtractHeaderType(header) ==  (byte)FrameType.Information)
                    .ElementAt(3);
                _bus.busData.IncomingframeData.Header = HeaderQuery;

                var MailQuery = _bus.SerialBuf.Read.Skip(4)
                    .ToArray();
                
                if(MailQuery.Length == 1) {
                    _bus.busData.IncomingframeData.Mail = new byte[1]{0};
                    _bus.busData.IncomingframeData.Checksum = _bus.busData.IncomingframeData.Header;

                    if(_bus.busData.IncomingframeData.Checksum != FrameBuilder.CalculateChecksum(_bus.busData.IncomingframeData)) {
                        Console.WriteLine("received incomplete frame");
                        return Err.InvalidMessage;
                    }
                    else 
                    {
                        FrameBreakdown();
                        return Err.Success;
                    }
                }
                else 
                {
                    _bus.busData.IncomingframeData.Mail = new byte[MailQuery.Length-1];
                    Array.Copy(MailQuery, _bus.busData.IncomingframeData.Mail, MailQuery.Length-1);

                    var ChecksumQuery = _bus.SerialBuf.Read.OfType<byte>()
                        .Where(checksum => checksum == FrameBuilder.CalculateChecksum(_bus.busData.IncomingframeData));
                    
                    if(ExtractFromQuery(ChecksumQuery) != FrameBuilder.CalculateChecksum(_bus.busData.IncomingframeData)) {
                        Console.WriteLine("received incomplete frame");
                        return Err.InvalidMessage;
                    }
                    else {
                        FrameBreakdown();
                        return Err.Success;
                    }
                }
            }
            else
            {
                return Err.Invalid;
            }
        }
        private void FrameBreakdown() {
            DebugPrint();
            switch (ExtractHeaderType(_bus.busData.IncomingframeData.Header)) {
                case (byte)FrameType.Unnumbered:
                    //Console.WriteLine("Type Unnumbered ");
                    if(_bus.busData.IncomingframeData.Header == 0xc0) {
                        _bus.Connected = true;
                    }
                    break;
                case (byte)FrameType.Information:
                    //Console.WriteLine("Type Information ");
                    break;
                case (byte)FrameType.Supervisory:
                    //Console.WriteLine("Type Supervisory ");
                    break;
            }
        }
        private void DebugPrint() {
            Debug.Write("-----------------------\n"+
                "inc frame\n"+
                "Header "+_bus.busData.IncomingframeData.Header.ToString("X")+"\n"+
                "Mail "+BitConverter.ToString(_bus.busData.IncomingframeData.Mail)+"\n"+
                "Checksum "+_bus.busData.IncomingframeData.Checksum.ToString("X")+"\n"+
                "-----------------------\n"
            );
        }
        /*
        internal void HandleFrameIncoming() {
            if(_bus.busData.PollFinal == true){
                TimerStart();
                Debug.WriteLine("Timer started.");
            }
            while(true){
                // scan for delimiter
                _bus.Read(1, 0);
                // check if the byte read is a frame delimeter
                if(_bus.SerialBuf.Read[0] == BusMailFrame.FrameChar){
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
                    switch (ExtractHeaderType(_bus.busData.IncomingframeData.Header)) {
                        case (byte)FrameType.Unnumbered:
                            Console.Write("Type Unnumbered ");
                            UnnumberedHeader(length, totallength);
                            break;
                        case (byte)FrameType.Information:
                            Console.Write("Type Information ");
                            InfoHeader(length, totallength);
                            break;
                   switch (ExtractHeaderType(_bus.busData.IncomingframeData.Header)) {
                case (byte)FrameType.Unnumbered:
                    Console.WriteLine("Type Unnumbered ");
                    if(_bus.busData.IncomingframeData.Header == 0xc0) {
                        _bus.Connected = true;
                    }
                    break;
                case (byte)FrameType.Information:
                    Console.WriteLine("Type Information ");
                    break;
                case (byte)FrameType.Supervisory:
                    Console.WriteLine("Type Supervisory ");
                    break;
            }
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
        */
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
