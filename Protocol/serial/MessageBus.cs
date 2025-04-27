using System.IO.Ports;
using System.Timers;
using System.Diagnostics;
using System.Net.Mime;

namespace Busmail
{
    public class MessageBus
    {
        internal SerialPort Serial { get; set; }
        public Buffer SerialBuf = new Buffer();
        public Data busData = new Data();
        internal bool Connected = false;
        internal bool FrameIncomplete = false;
        internal System.Timers.Timer _retxTimer = new (interval: 1000);
        internal MessageBusTransmit Transmit;
        internal MessageBusReceive Receive;
        internal readonly object _buslock = new object();
        internal MessageBus(int baudRate = 115200)
        {
            string[] ports = SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                Console.Write("No ports found");
                Environment.Exit(1);
            }
            Serial = new SerialPort(ports[0], baudRate);
            Transmit = new MessageBusTransmit(this);
            Receive = new MessageBusReceive(this);
            Init();
            Transmit.Connect();
        }
        private void Init(){
            Serial.Handshake = Handshake.None;
            Serial.DataReceived += new SerialDataReceivedEventHandler(HandleDataReceived);
            Serial.Open();
        }
        private void HandleDataReceived(object sender, SerialDataReceivedEventArgs e) {
            //if(FrameIncomplete == true)
            //   BusOut.Retransmit();
            var sp = (SerialPort)sender;
            int length = sp.BytesToRead;
            if(length > 2){
                SerialBuf.Read = new byte[length];
                lock(_buslock){Read(length, 0);}
                //Console.WriteLine("content read: "+BitConverter.ToString(SerialBuf.Read));
                lock(_buslock){Receive.HandleSerialBuffer();}
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
            SerializeFrame(frame);  // Make our frame object serial bus-friendly :smiling_face_with_3_hearts:
            busData.SavedFrame = frame; // Save message for retransmission
            // Actually sending message
            if(SerialBuf.Write != null)
                Serial.Write(SerialBuf.Write, 0, SerialBuf.Write.Length);
            
            Thread.Sleep(50);   // Dio santo che sei nei cieli sia santificato il tuo nome, sia in cielo sia in terra. dacci oggi il nostro pane quotidiano..
            // Oh shit è morto il papa
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

    internal class MessageBusTransmit
    {
        private readonly MessageBus _bus;
        internal MessageBusTransmit(MessageBus bus) {
            _bus = bus;
        }
        public void SyncFrame(bool pf = false) {
                var sabmFrame = FrameBuilder.BuildFrame(FrameType.Unnumbered, null, pf);
                _bus.busData.PollFinal = true;
                _bus.Write(sabmFrame);
        }
        public void InfoFrame(ushort primitive, bool pf = false, byte[] parameters = null) {
            // create mail length bytes
            var LengthSegment = sizeof(ushort);
            // assign size of parameters to the value
            if(parameters != null) LengthSegment += parameters.Length;
            // save pf value of current frame to global PollFinal value
            _bus.busData.PollFinal = pf;
            // build the Mail part of the frame
            byte[] data = new byte[LengthSegment];
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
        // Attempt to retransmit messages using a single function, unsure how to implement this atm
        internal void Retransmit() {
            if(_bus.busData.Lost == 6) {
                _bus.busData.PollFinal = false;
                _bus._retxTimer.Stop();
                Debug.WriteLine("Timer elapsed (max lost packets count)");
            } else {
                    _bus.busData.Lost++;                if (mailQuery.Length <= 1)
                {
                    _bus.busData.IncomingframeData.Mail = [0];
                    _bus.busData.IncomingframeData.Checksum = _bus.busData.IncomingframeData.Header;
                } 
                else 
                {
                    _bus.busData.IncomingframeData.Mail = new byte[mailQuery.Length-1];
                    Array.Copy(mailQuery, _bus.busData.IncomingframeData.Mail, mailQuery.Length-1);

                    var checksumQuery = _bus.SerialBuf.Read.OfType<byte>()
                        .Where(checksum => checksum == FrameBuilder.CalculateChecksum(_bus.busData.IncomingframeData));
                    _bus.busData.IncomingframeData.Checksum = ExtractFromQuery(checksumQuery);

                    ValidateChecksum(ExtractFromQuery(checksumQuery));
                }

            }
            Debug.Write("Retransmitting frame: ");
            _bus.Write(_bus.busData.SavedFrame);
            Thread.Sleep(1000);
        }
        // Attempt to retransmit messages involving a timer, unused for now
        /*
        internal void TimerStart() {
            Debug.WriteLine("Attemtpting to retransmit frames, starting timer");
            _bus._retxTimer.Elapsed += (sender, e) => _bus.Transmit.Retransmit(sender!, e);
            _bus._retxTimer.Enabled = true;
            _bus._retxTimer.Start();
        }
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
                        // Retransmit until Unnumbered is frame received
                        Retransmit();
                    }
                }
            }
        }
    }
    internal class MessageBusReceive 
    {
        private readonly MessageBus _bus;
        internal MessageBusReceive(MessageBus bus) {
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
        private Err CheckLength(int length)
        {
            if (length != _bus.busData.IncomingframeData.Length + 1)
            {
                Console.Write("Invalid length! ");
                return Err.Invalid;
            }
            return Err.Success;
        }
        private Err ValidateChecksum(byte checksum)
        {
            if (checksum != FrameBuilder.CalculateChecksum(_bus.busData.IncomingframeData))
            {
                Console.Write("Invalid checksum!\n");
                return Err.InvalidMessage;
            }
            return Err.Success;
        }
        internal Err HandleSerialBuffer() {
            // find delimiter first, do the rest if it's present
            var queryFrameChar = _bus.SerialBuf.Read.OfType<byte>().Where(delimiter => delimiter == BusMailFrame.FrameChar);
            var delimiter = ExtractFromQuery<byte>(queryFrameChar);
            if(delimiter != 0) {
                var length = _bus.SerialBuf.Read.OfType<byte>().SkipWhile(length => length != delimiter).ElementAt(2);
                _bus.busData.IncomingframeData.Length = (ushort)(length - 1);

                var headerQuery = _bus.SerialBuf.Read.OfType<byte>()
                    .Where(header => ExtractHeaderType(header) == (byte)FrameType.Unnumbered  
                                || ExtractHeaderType(header) == (byte)FrameType.Supervisory 
                                || ExtractHeaderType(header) ==  (byte)FrameType.Information)
                    .ElementAt(3);
                _bus.busData.IncomingframeData.Header = headerQuery;

                var mailQuery = _bus.SerialBuf.Read.Skip(4)
                    .ToArray();

                var ret = CheckLength(mailQuery.Length);
                if (ret == Err.Invalid)
                {
                    return ret;
                }
                
                if (mailQuery.Length <= 1)
                {
                    _bus.busData.IncomingframeData.Mail = [0];
                    _bus.busData.IncomingframeData.Checksum = _bus.busData.IncomingframeData.Header;
                } 
                else 
                {
                    _bus.busData.IncomingframeData.Mail = new byte[mailQuery.Length-1];
                    Array.Copy(mailQuery, _bus.busData.IncomingframeData.Mail, mailQuery.Length-1);

                    var checksumQuery = _bus.SerialBuf.Read.OfType<byte>()
                        .Where(checksum => checksum == FrameBuilder.CalculateChecksum(_bus.busData.IncomingframeData));
                    _bus.busData.IncomingframeData.Checksum = ExtractFromQuery(checksumQuery);

                    ValidateChecksum(ExtractFromQuery(checksumQuery));
                }
                FrameBreakdown();
            }
            return Err.Invalid;
        }
        private void FrameBreakdown()
        {
            DebugPrint();
            switch (ExtractHeaderType(_bus.busData.IncomingframeData.Header)) {
                case (byte)FrameType.Unnumbered:
                    //Console.WriteLine("Type Unnumbered ");
                    _bus.busData.IncomingFrameType = "Unnumbered";
                    if(_bus.busData.IncomingframeData.Header == 0xc0) {
                        _bus.Connected = true;
                    }
                    break;
                case (byte)FrameType.Information:
                    _bus.busData.IncomingFrameType = "Information";
                    //Console.WriteLine("Type Information ");
                    break;
                case (byte)FrameType.Supervisory:
                    _bus.busData.IncomingFrameType = "Supervisory";
                    //Console.WriteLine("Type Supervisory ");
                    break;
            }
        }
        private void DebugPrint() {
            Console.Write("-----------------------\n"+
                "incoming frame\n"+
                "Type: "+_bus.busData.IncomingFrameType+"\n"+
                "Header "+_bus.busData.IncomingframeData.Header.ToString("X")+"\n"+
                "Mail "+BitConverter.ToString(_bus.busData.IncomingframeData.Mail)+"\n"+
                "Checksum "+_bus.busData.IncomingframeData.Checksum.ToString("X")+"\n"+
                "-----------------------\n"
            );
        }
    }
}