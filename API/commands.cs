using Busmail;
using HALAPI = API.API_HAL.API_HAL;
using FPGen = API.API_FP_GENERAL.API_FP_GENERAL;

namespace API.COMMANDS {
    public class HAL {
        private readonly HALAPI _hal;
        public HAL(MessageBus bus) {
            this._hal =  new HALAPI(bus);
        }
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
        public void Blink(int duration, int repeat) {
            Command[] blinkcmd =
            [
                _hal.ApiHalLedCmd(HALAPI.ApiHalLedCmdIdType.ALI_LED_ON, duration), 
                _hal.ApiHalLedCmd(HALAPI.ApiHalLedCmdIdType.ALI_LED_OFF, duration), 
                _hal.ApiHalLedCmd(HALAPI.ApiHalLedCmdIdType.ALI_LED_REPEAT_SEQUENCE, repeat)
            ];
            HALAPI.ApiHalLedReq(_hal, 2, blinkcmd);
        }
    }
    public class FP_GENERAL {
        private readonly FPGen _fpgen;
        public FP_GENERAL(MessageBus bus) {
            this._fpgen =  new FPGen(bus);
        }
        public void Version() {
            // Send command
            FPGen.ApiFpGeneralGetVersion(_fpgen);
        }
    }
}