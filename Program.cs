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
        MessageBusHandle.InitializeConnection(bus);
        Thread.Sleep(500);
        MessageBusHandle.InfoFrame(bus, (ushort)0x4002, true);
        //MessageBusHandle.InfoFrame(bus, (ushort)API_HAL_CMD.API_HAL_LED_RQ, true, Params.ToArray());
    }
}