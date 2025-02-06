using Busmail;

namespace API {
    public enum RsStatusType : byte
    {
        RSS_SUCCESS,
        RSS_NOT_SUPPORTED,
        RSS_BAD_ARGUMENTS,
        RSS_BAD_ADDRESS,
        RSS_BAD_FUNCTION,
        RSS_BAD_HANDLE,
        RSS_BAD_DATA,
        RSS_BAD_LENGTH,
        RSS_NO_MEMORY,
        RSS_NO_DEVICE,
        RSS_NO_DATA,
        RSS_RETRY,
        RSS_NOT_READY,
        RSS_IO,
        RSS_CRC,
        RSS_CANCELLED,
        RSS_RESET = 0x00,
        RSS_PENDING = 0x11,
        RSS_BUSY,
        RSS_TIMEOUT,
        RSS_OVERFLOW,
        RSS_NOT_FOUND,
        RSS_STALLED,
        RSS_DENIED,
        RSS_REJECTED,
        RSS_AMBIGOUS,
        RSS_NO_RESOURCE,
        RSS_NOT_CONNECTED,
        RSS_OFFLINE,
        RSS_REMOTE_ERROR,
        RSS_NO_CAPABILITY,
        RSS_FILE_ACCESS,
        RSS_DUPLICATE,
        RSS_LOGGED_OUT,
        RSS_ABNORMAL_TERMINATION,
        RSS_FAILED,
        RSS_UNKNOWN,
        RSS_BLOCKED,
        RSS_NOT_AUTHORIZED,
        RSS_PROXY_CONNECT,
        RSS_INVALID_PASSWORD,
        RSS_FORBIDDEN,
        RSS_SPARE_2A,
        RSS_SPARE_2B,
        RSS_SPARE_2C,
        RSS_SPARE_2D,
        RSS_SPARE_2E,
        RSS_SPARE_2F,
        RSS_UNAVAILABLE,
        RSS_NETWORK,
        RSS_NO_CREDITS,
        RSS_LOW_CREDITS,
        RSS_MAX = 0xFF
    }

    public class command
    {
        public List<byte> fields = new List<byte>();
    }
    
    public class API_HAL
    {
        public enum Command : ushort
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

        public static void ApiHalReadReq(MessageBus bus, ApiHalAreaType area, uint Address, ushort Length)
        {
            command apiHalRead = new command(); 
            apiHalRead.fields.Add((byte)area);
            apiHalRead.fields.Add((byte)Address);
            apiHalRead.fields.Add((byte)(Address >> 8));
            apiHalRead.fields.Add((byte)(Address >> 0x10));
            apiHalRead.fields.Add((byte)(Address >> 0x18));
            apiHalRead.fields.Add((byte)(Length & 0xFF));
            apiHalRead.fields.Add((byte)(Length >> 8));
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_HAL.Command.READ_REQ, false, apiHalRead.fields.ToArray());
        }

        public static void ApiHalWrite()
        {
            
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

        public static void ApiHalDeviceControlReq(MessageBus bus, ApiHalDeviceControlType controlType, ApiHalDeviceIdType idType)
        {
            command apiHalDeviceControlReq = new command();
            apiHalDeviceControlReq.fields.Add((byte)idType);
            apiHalDeviceControlReq.fields.Add((byte)controlType);
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_HAL.Command.DEVICE_CONTROL_REQ, false, apiHalDeviceControlReq.fields.ToArray());
        }
        
        public enum ApiHalLedCmdIdType : byte
        {
            ALI_LED_OFF,
            ALI_LED_ON,
            ALI_LED_REPEAT_SEQUENCE,
            ALI_LED_INVALID = 0xFF
        }

        public static void ApiHalLedReq(MessageBus bus, int Led, command[] commands)
        {
            List<byte> Params = new List<byte>() {(byte)Led, (byte)commands.Length};
            foreach (command cmd in commands)
            {
                Params.Add(cmd.fields[0]);
                Params.Add(cmd.fields[1]);
                Params.Add(cmd.fields[2]);
            }
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_HAL.Command.LED_REQ, false, Params.ToArray());
        }
        
        public static command ApiHalLedCmd(ApiHalLedCmdIdType id, int duration)
        {
            command apiHalLedCmd = new command();
            apiHalLedCmd.fields.Add((byte)id);
            apiHalLedCmd.fields.Add((byte)(duration & 0xFF));
            apiHalLedCmd.fields.Add((byte)(duration >> 8));
            return apiHalLedCmd;
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

        
    }
}