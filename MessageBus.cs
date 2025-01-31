using System.IO.Ports;

namespace Busmail
{
    public class MessageBus
    {
        internal SerialPort Serial { get; private set; }

        public MessageBus(string portName = "COM0", int baudRate = 115200)
        {
            Serial = new SerialPort(portName, baudRate);
        }

        public Err Read()
        {
            return Err.Success;
        }

        public Err Write()
        {
            return Err.Success;
        }
    }
}
