using Busmail;

namespace API.API_HAL {
    public class API_HAL
    {
        private readonly MessageBus _bus;
        public API_HAL(MessageBus bus) {
            _bus = bus;
        }
        internal enum command : ushort
        {
            DEVICE_CONTROL_REQ = 0x5900,
            DEVICE_CONTROL_CFM,
            LED_REQ,
            LED_CFM,
            READ_REQ,
            READ_CFM,
            WRITE_REQ,
            WRITE_CFM,
            GPIO_FN_REGISTER_REQ = 0x5910,
            GPIO_FN_REGISTER_CFM,
            SET_GPIO_PORT_PIN_MODE_REQ,
            SET_GPIO_PORT_PIN_MODE_CFM,
            SET_GPIO_PORT_REQ,
            SET_GPIO_PORT_CFM,
            RESET_GPIO_PORT_REQ,
            RESET_GPIO_PORT_CFM,
            GET_GPIO_PORT_REQ,
            GET_GPIO_PORT_CFM
        }

        public enum ApiHalDeviceIdType
        {
            AHD_NONE,
            AHD_UART1,
            AHD_UART2,
            AHD_SPI1,
            AHD_SPI2,
            AHD_TIM0,
            AHD_MAX
        }

        public enum ApiHalDeviceControlType
        {
            AHC_NULL,
            AHC_DISABLE,
            AHC_ENABLE,
            AHC_MAX
        }
        
        public enum ApiHalLedCmdIdType : byte
        {
            ALI_LED_OFF,
            ALI_LED_ON,
            ALI_LED_REPEAT_SEQUENCE,
            ALI_LED_INVALID = 0xFF
        }
        
        public enum ApiHalAreaType : byte
        {
            AHA_MEMORY,
            AHA_REGISTER,
            AHA_NVS,
            AHA_DSP,
            AHA_FPGA,
            AHA_SEQUENCER
        }

        internal enum ApiGpioPortPinModeType : byte
        {
            GPIO_PORT_PIN_MODE_INPUT,
            GPIO_PORT_PIN_MODE_INPUT_PULL_UP,
            GPIO_PORT_PIN_MODE_INPUT_PULL_DOWN,
            GPIO_PORT_PIN_MODE_OUTPUT
        }
        internal enum ApiGpioPortPinType : byte
        {
            GPIO_PORT_P0_0,
            GPIO_PORT_P0_1,
            GPIO_PORT_P0_2,
            GPIO_PORT_P0_3,
            GPIO_PORT_P0_4,
            // etc etc.
            GPIO_PORT_INVALID = 0xff
        }
        internal enum ApiGpioPortType : byte
        {
            GPIO_PORT_P0,
            GPIO_PORT_P1,
            GPIO_PORT_P2,
            GPIO_PORT_P3
        }
        internal enum ApiGpioPinType : byte
        {
            GPIO_PIN_0,
            GPIO_PIN_1,
            GPIO_PIN_2,
            GPIO_PIN_3,
            GPIO_PIN_4,
            GPIO_PIN_5,
            GPIO_PIN_6,
            GPIO_PIN_7
        }
        // Types above come from the PP document

        internal void ApiHalDeviceControlReq(ApiHalDeviceControlType controlType, ApiHalDeviceIdType idType)
        {
            var apiHalDeviceControlReq = new Command();
            apiHalDeviceControlReq.fields.Add((byte)idType);
            apiHalDeviceControlReq.fields.Add((byte)controlType);
            _bus.BusOut.InfoFrame((ushort)command.DEVICE_CONTROL_REQ, false, apiHalDeviceControlReq.fields.ToArray());
        }
        internal void ApiHalLedReq(int led, Command[] Commands)
        {
            Console.Write("sending LedReq command: ");
            List<byte> parameters = new List<byte>() {(byte)led, (byte)Commands.Length};
            foreach (var cmd in Commands)
            {
                parameters.Add(cmd.fields[0]);
                parameters.Add(cmd.fields[1]);
                parameters.Add(cmd.fields[2]);
            }
            _bus.BusOut.InfoFrame((ushort)command.LED_REQ, false, parameters.ToArray());
        }
        internal Command ApiHalLedCmd(ApiHalLedCmdIdType id, int duration)
        {
            var apiHalLedCmd = new Command();
            apiHalLedCmd.fields.Add((byte)id);
            apiHalLedCmd.fields.Add((byte)(duration & 0xFF));
            apiHalLedCmd.fields.Add((byte)(duration >> 8));
            return apiHalLedCmd;
        }
        internal void ApiHalReadReq(ApiHalAreaType area, uint address, ushort length)
        {
            var apiHalRead = new Command(); 
            apiHalRead.fields.Add((byte)area);
            apiHalRead.fields.Add((byte)address);
            apiHalRead.fields.Add((byte)(address >> 8));
            apiHalRead.fields.Add((byte)(address >> 0x10));
            apiHalRead.fields.Add((byte)(address >> 0x18));
            apiHalRead.fields.Add((byte)(length & 0xFF));
            apiHalRead.fields.Add((byte)(length >> 8));
            //_busout.InfoFrame((ushort)command.READ_REQ, false, apiHalRead.fields.ToArray());
        }

        internal void ApiHalWriteReq(ApiHalAreaType area, uint address, ushort length)
        {
            var apiHalRead = new Command(); 
            apiHalRead.fields.Add((byte)area);
            apiHalRead.fields.Add((byte)address);
            apiHalRead.fields.Add((byte)(address >> 8));
            apiHalRead.fields.Add((byte)(address >> 0x10));
            apiHalRead.fields.Add((byte)(address >> 0x18));
            apiHalRead.fields.Add((byte)(length & 0xFF));
            apiHalRead.fields.Add((byte)(length >> 8));
            //_busout.InfoFrame((ushort)command.WRITE_REQ, false, apiHalRead.fields.ToArray());
        }
        
        // more functions can be added as needed
        
    }

}