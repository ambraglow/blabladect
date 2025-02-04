using System.IO.Ports;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.VisualBasic;

namespace Busmail
{
    public class MessageBus
    {
        public static byte[] ReadBus;
        public static byte[] WriteBus;

        internal SerialPort Serial { get; private set; }

        public MessageBus(string portName = "COM0", int baudRate = 115200)
        {
            Serial = new SerialPort(portName, baudRate);
        }

        public Err Read(ref byte[] data)
        {
            //Serial.Read(ref data, 0, );
            return Err.Success;
        }
        public Err Write(byte[] data)
        {
            Serial.Write(data, 0, data.Length);
            return Err.Success;
        }
    }
}
