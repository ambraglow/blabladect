using Busmail;

namespace API.API_FP_GENERAL {
    public class API_FP_GENERAL
    {
        private readonly MessageBus _bus;
        public API_FP_GENERAL(MessageBus bus) {
            _bus = bus;
        }
        internal enum command : ushort
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
                
        public void ApiFpGeneralReset()
        {
            _bus.Connected = false;
            _bus.busData.PollFinal = true;
            _bus.BusOut.InfoFrame((ushort)API_FP_GENERAL.command.RESET_REQ);
        }

        public void ApiFpGeneralGetVersion()
        {
            Console.Write("Sending GetVersion command: ");
            _bus.BusOut.InfoFrame((ushort)API_FP_GENERAL.command.GET_FW_VERSION_REQ, false);
        }

        public static void ApiFpGeneralSetTime(API_FP_GENERAL fpgen, ApiTimeDateCodingType coding, ApiTimeDateInterpretationType interpretation, ApiTimeDateCodeType timestamp)
        {
            var apiFpGeneralSetTime = new Command();
            apiFpGeneralSetTime.fields.Add((byte)coding);
            apiFpGeneralSetTime.fields.Add((byte)interpretation);
            foreach (var elements in Enum.GetValues(typeof(ApiTimeDateCodingType)))
            {
                apiFpGeneralSetTime.fields.Add((byte)elements);
            }
            Console.Write("Sending ApiFpGeneralSetTime Command: ");
            fpgen._bus.BusOut.InfoFrame((ushort)API_FP_GENERAL.command.SET_TIME_REQ, false,apiFpGeneralSetTime.fields.ToArray());
        }

        public static void ApiFpGeneralGetTime()
        {
            Console.Write("Sending ApiFpGeneralGetTime Command: ");
            //MessageBus.BusOut.InfoFrame((ushort)API_FP_GENERAL.command.GET_TIME_REQ, false);
        }

        public static void ApiFpGeneralSetTimeInd(ApiTerminalIdType terminalId, ApiTimeDateCodingType coding, ApiTimeDateInterpretationType interpretation, ApiTimeDateCodeType timestamp)
        {
            var apiFpGeneralSetTimeInd = new Command();
            apiFpGeneralSetTimeInd.fields[0] = (byte)terminalId;
            apiFpGeneralSetTimeInd.fields[1] = (byte)coding;
            apiFpGeneralSetTimeInd.fields[2] = (byte)interpretation;
            foreach (var elements in Enum.GetValues(typeof(ApiTimeDateCodingType)))
            {
                apiFpGeneralSetTimeInd.fields.Add((byte)elements);
            }
            //MessageBus.BusOut.InfoFrame((ushort)API_FP_GENERAL.command.SET_TIME_IND, false,apiFpGeneralSetTimeInd.fields.ToArray());
        }
    }

}