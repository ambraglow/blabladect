using System.ComponentModel.Design;
using System.Transactions;
using Busmail;

namespace API {
    public class GeneralTypes
    {
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
    }

    public class command
    {
        public List<byte> fields = new List<byte>();
    }

    public class API_FP_GENERAL
    {
        private enum Command : ushort
        {
            // Management
            RESET_REQ = 0x4000,
            RESET_IND,
            GET_FW_VERSION_REQ,
            GET_FW_VERSION_CFM,
            SET_CRADLE_STATUS_REQ = 0x407F,
            SET_CRADLE_STATUS_IND,
            // RTC
            SET_TIME_REQ = 0x4082,
            SET_TIME_CFM,
            GET_TIME_REQ,
            GET_TIME_CFM,
            SET_TIME_IND,
            SYNC_TIME_REQ,
            // API settings
            SET_FEATURES_REQ = 0x40A0,
            SET_FEATURES_CFM,
            GET_FEATURES_REQ,
            GET_FEATURES_CFM
        }

        public enum ApiTerminalIdType: ushort
        {
            API_TERMINAL_ID_BROADCAST_AUDIO = 0xFFFD,
            API_TERMINAL_ID_ALL_PP,
            API_TERMINAL_ID_INVALID
        }

        public struct ApiModelIdType
        {
            private ushort MANIC;
            private ushort MODIC;
        }

        public enum ApiDectTypeType : byte
        {
            API_EU_DECT,
            API_US_DECT,
            API_SA_DECT,
            API_TAIWAN_DECT,
            API_MALAYSIA_DECT,
            API_CHINA_DECT,
            API_THAILAND_DECT,
            API_BRAZIL_DECT,
            API_US_DECT_EXT_FREQ,
            API_KOREAN_DECT,
            API_JAPAN_DECT,
            API_JAPAN_DECT_5CH,
            API_DECT_TYPE_INVALID = 0xFF
        }

        public enum ApiCradleStatusType : byte
        {
            API_CRADLE_INACTIVE,
            API_CRADLE_ACTIVE,
            API_CRADLE_UNSUPPORTED,
            API_CRADLE_INVALID = 0xFF
        }

        public struct ApiTimeDateCodeType
        {
            public byte Year;
            public byte Month;
            public byte Day;
            public byte Hour;
            public byte Minute;
            public byte Second;
            public byte TimeZone;
        }

        public enum ApiTimeDateInterpretationType : byte
        {
            API_INTER_CURRENT_TIME_DATE,
            API_INTER_TIME_DURATION,
            API_INTER_DELIVER_MMS_MSG = 0x20,
            API_INTER_MMS_DATA_CREATED,
            API_INTER_MMS_DATA_MODIFIED,
            API_INTER_DATA_MCE_DATA_RECIEVED,
            API_INTER_DATA_ACCESSED_END,
            API_INTER_TIME_DATE_STAMP_ID
        }

        public enum ApiTimeDateCodingType : byte
        {
            API_CODING_TIME,
            API_CODING_DATE,
            API_CODING_TIME_DATE
        }
                
        public static void ApiFpGeneralReset(MessageBus bus)
        {
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_FP_GENERAL.Command.RESET_REQ);
        }

        public static void ApiFpGeneralGetVersion(MessageBus bus)
        {
            Console.Write("Sending GetVersion command: ");
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_FP_GENERAL.Command.GET_FW_VERSION_REQ, false);
        }

        public static void ApiFpGeneralSetTime(MessageBus bus, ApiTimeDateCodingType coding, ApiTimeDateInterpretationType interpretation, ApiTimeDateCodeType timestamp)
        {
            var apiFpGeneralSetTime = new command();
            apiFpGeneralSetTime.fields.Add((byte)coding);
            apiFpGeneralSetTime.fields.Add((byte)interpretation);
            foreach (var elements in Enum.GetValues(typeof(ApiTimeDateCodingType)))
            {
                apiFpGeneralSetTime.fields.Add((byte)elements);
            }
            Console.Write("Sending ApiFpGeneralSetTime command: ");
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_FP_GENERAL.Command.SET_TIME_REQ, false,apiFpGeneralSetTime.fields.ToArray());
        }

        public static void ApiFpGeneralGetTime(MessageBus bus)
        {
            Console.Write("Sending ApiFpGeneralGetTime command: ");
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_FP_GENERAL.Command.GET_TIME_REQ, false);
        }

        public static void ApiFpGeneralSetTimeInd(MessageBus bus, ApiTerminalIdType terminalId, ApiTimeDateCodingType coding, ApiTimeDateInterpretationType interpretation, ApiTimeDateCodeType timestamp)
        {
            var apiFpGeneralSetTimeInd = new command();
            apiFpGeneralSetTimeInd.fields[0] = (byte)terminalId;
            apiFpGeneralSetTimeInd.fields[1] = (byte)coding;
            apiFpGeneralSetTimeInd.fields[2] = (byte)interpretation;
            foreach (var elements in Enum.GetValues(typeof(ApiTimeDateCodingType)))
            {
                apiFpGeneralSetTimeInd.fields.Add((byte)elements);
            }
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_FP_GENERAL.Command.SET_TIME_IND, false,apiFpGeneralSetTimeInd.fields.ToArray());
        }
    }
    
    public class API_HAL
    {
        private enum Command : ushort
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

        public static void ApiHalDeviceControlReq(MessageBus bus, ApiHalDeviceControlType controlType, ApiHalDeviceIdType idType)
        {
            var apiHalDeviceControlReq = new command();
            apiHalDeviceControlReq.fields.Add((byte)idType);
            apiHalDeviceControlReq.fields.Add((byte)controlType);
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_HAL.Command.DEVICE_CONTROL_REQ, false, apiHalDeviceControlReq.fields.ToArray());
        }
        public static void ApiHalLedReq(MessageBus bus, int led, command[] commands)
        {
            Console.Write("sending LedReq command: ");
            List<byte> parameters = new List<byte>() {(byte)led, (byte)commands.Length};
            foreach (var cmd in commands)
            {
                parameters.Add(cmd.fields[0]);
                parameters.Add(cmd.fields[1]);
                parameters.Add(cmd.fields[2]);
            }
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_HAL.Command.LED_REQ, false, parameters.ToArray());
        }
        public static command ApiHalLedCmd(ApiHalLedCmdIdType id, int duration)
        {
            var apiHalLedCmd = new command();
            apiHalLedCmd.fields.Add((byte)id);
            apiHalLedCmd.fields.Add((byte)(duration & 0xFF));
            apiHalLedCmd.fields.Add((byte)(duration >> 8));
            return apiHalLedCmd;
        }
        public static void ApiHalReadReq(MessageBus bus, ApiHalAreaType area, uint address, ushort length)
        {
            var apiHalRead = new command(); 
            apiHalRead.fields.Add((byte)area);
            apiHalRead.fields.Add((byte)address);
            apiHalRead.fields.Add((byte)(address >> 8));
            apiHalRead.fields.Add((byte)(address >> 0x10));
            apiHalRead.fields.Add((byte)(address >> 0x18));
            apiHalRead.fields.Add((byte)(length & 0xFF));
            apiHalRead.fields.Add((byte)(length >> 8));
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_HAL.Command.READ_REQ, false, apiHalRead.fields.ToArray());
        }

        public static void ApiHalWriteReq(MessageBus bus, ApiHalAreaType area, uint address, ushort length)
        {
            var apiHalRead = new command(); 
            apiHalRead.fields.Add((byte)area);
            apiHalRead.fields.Add((byte)address);
            apiHalRead.fields.Add((byte)(address >> 8));
            apiHalRead.fields.Add((byte)(address >> 0x10));
            apiHalRead.fields.Add((byte)(address >> 0x18));
            apiHalRead.fields.Add((byte)(length & 0xFF));
            apiHalRead.fields.Add((byte)(length >> 8));
            MessageBusOutgoing.InfoFrame(bus, (ushort)API_HAL.Command.WRITE_REQ, false, apiHalRead.fields.ToArray());
        }
        
        // more functions can be added as needed
        
    }
    
}