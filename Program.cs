using Busmail;
using API.COMMANDS;

class Program
{
    static void Main()
    {
        MessageBus bus = new MessageBus();
        HAL halcmd = new HAL(bus);
        FP_GENERAL fpgcmd = new FP_GENERAL(bus);

        //fpgcmd.software_Reset();
        halcmd.Blink(200, 2);
        fpgcmd.Version();
    }
}