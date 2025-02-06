using System;
using Busmail;
using API;

class Program
{
    static void Main()
    {
        MessageBus bus = new MessageBus();
        Thread.Sleep(100);
        MessageBus.InitializeConnection(bus);
        Thread.Sleep(500);
        
        API_FP_GENERAL.ApiFpGeneralGetVersion(bus);
        
        API.command[] blink =
        [
            API_HAL.ApiHalLedCmd(API_HAL.ApiHalLedCmdIdType.ALI_LED_ON, 500), 
                API_HAL.ApiHalLedCmd(API_HAL.ApiHalLedCmdIdType.ALI_LED_OFF, 500), 
                API_HAL.ApiHalLedCmd(API_HAL.ApiHalLedCmdIdType.ALI_LED_REPEAT_SEQUENCE, 10)
        ];
        API_HAL.ApiHalLedReq(bus, 2, blink);
        
        API_FP_GENERAL.ApiFpGeneralGetTime(bus);
    }
}