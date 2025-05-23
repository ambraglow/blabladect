using Busmail;
using HALAPI = API.API_HAL.API_HAL;
using FPGen = API.API_FP_GENERAL.API_FP_GENERAL;
using FPMM = API.API_FP_MM.API_FP_MM;

namespace API.COMMANDS {
    public class HAL {
        private readonly HALAPI _hal;
        internal HAL(MessageBus bus) {
            _hal = new HALAPI(bus);
        }

        public void Blink(int duration, int repeat) {
            Command[] blinkcmd =
            [
                _hal.ApiHalLedCmd(HALAPI.ApiHalLedCmdIdType.ALI_LED_ON, duration), 
                _hal.ApiHalLedCmd(HALAPI.ApiHalLedCmdIdType.ALI_LED_OFF, duration), 
                _hal.ApiHalLedCmd(HALAPI.ApiHalLedCmdIdType.ALI_LED_REPEAT_SEQUENCE, repeat)
            ];
            _hal.ApiHalLedReq(2, blinkcmd);
        }
    }
    public class FP_GENERAL {
        private readonly FPGen _fpgen;
        internal FP_GENERAL(MessageBus bus) {
            _fpgen = new FPGen(bus);
        }
        public void Version() {
            // Send command
            _fpgen.ApiFpGeneralGetVersion();
        }
        public void software_Reset() {
            _fpgen.ApiFpGeneralReset();
        }
    }
    public class FP_MM {
        private readonly FPMM _fpmm;
        internal FP_MM(MessageBus bus) {
            _fpmm = new FPMM(bus);
        }
        public void RequestFPID() {
            _fpmm.ApiFpMmGetIdReq();
        }
    }

    // nvm this garbage i was testing stuff
    /*
        internal static string readcommandresponse(MessageBus bus) {
            if(bus.busData.IncomingframeData.Mail.Length <= 1) return "";
            ushort CommandResponse = (ushort)(bus.busData.IncomingframeData.Mail[3] << 8);
            CommandResponse |= bus.busData.IncomingframeData.Mail[2];
            var ResponseName = Enum.GetName(typeof(HAL.command), CommandResponse);
            //Console.WriteLine(CommandResponse.ToString("X"));
            if(ResponseName != null) {
                return ResponseName;
            } else {
                return "";
            }
        }
    */
}