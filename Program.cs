using System;
using Busmail;
using API;

class Program
{
    static ReadOnlySpan<byte> Params => [0x02, 0x03, 0x01, 0x2C, 0x01, 0x00, 0x2C, 0x01, 0x02, 0x0A, 0x00];

    static void Main()
    {
        MessageBus bus = new MessageBus();
        Thread.Sleep(100);
        MessageBus.InitializeConnection(bus);
        Thread.Sleep(500);
        //MessageBusHandle.InfoFrame(bus, (ushort)0x4002, true);
        //MessageBusHandle.InfoFrame(bus, (ushort)API_HAL.CMD.LED_REQ, true, Params.ToArray());
        
        API.command[] blink =
        [
            API_HAL.ApiHalLedCmd(API_HAL.ApiHalLedCmdIdType.ALI_LED_ON, 500), 
                API_HAL.ApiHalLedCmd(API_HAL.ApiHalLedCmdIdType.ALI_LED_OFF, 500), 
                API_HAL.ApiHalLedCmd(API_HAL.ApiHalLedCmdIdType.ALI_LED_REPEAT_SEQUENCE, 10)
        ];
        API_HAL.ApiHalLedReq(bus, 2, blink);

        API_HAL.ApiHalReadReq(bus, API_HAL.ApiHalAreaType.AHA_REGISTER, 0xFF481E, 2);
    }
}