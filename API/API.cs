using Busmail;
using HAL = API.API_HAL.API_HAL;
using FPGen = API.API_FP_GENERAL.API_FP_GENERAL;

namespace API {
    public class API {
        public FPGen fpgen;
        public HAL hal;
        public API(MessageBus bus) {
            hal = new HAL(bus);
            fpgen = new FPGen(bus);
        }
    }
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
    public class Command
    {
        public List<byte> fields = new List<byte>();
    }    
}