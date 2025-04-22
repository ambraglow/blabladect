using System;
using Busmail;
using API;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        MessageBus bus = new MessageBus();
        Thread.Sleep(100);
        MessageBus.Connect(bus);
        Thread.Sleep(100);
        API_FP_GENERAL.ApiFpGeneralGetVersion(bus);
        
        API.Command[] blink =
        [
            API_HAL.ApiHalLedCmd(API_HAL.ApiHalLedCmdIdType.ALI_LED_ON, 500), 
                API_HAL.ApiHalLedCmd(API_HAL.ApiHalLedCmdIdType.ALI_LED_OFF, 500), 
                API_HAL.ApiHalLedCmd(API_HAL.ApiHalLedCmdIdType.ALI_LED_REPEAT_SEQUENCE, 10)
        ];
        API_HAL.ApiHalLedReq(bus, 2, blink);
        
        API_FP_MM.ApiFpMmGetIdReq(bus);
        /*
        API_FP_GENERAL.ApiFpGeneralGetTime(bus);

        API_FP_GENERAL.ApiTimeDateCodeType time;
        time.Day = 0x16;
        time.Hour = 0x3;
        time.Minute = 0x27;
        time.Second = 0x10;
        time.Year = 0x19;
        time.Month = 0x4;
        time.TimeZone = 0x01;

        API_FP_GENERAL.ApiFpGeneralSetTime(bus, API_FP_GENERAL.ApiTimeDateCodingType.API_CODING_TIME_DATE, API_FP_GENERAL.ApiTimeDateInterpretationType.API_INTER_CURRENT_TIME_DATE, time);
   
        API_FP_GENERAL.ApiFpGeneralGetTime(bus);
        */
    }
}