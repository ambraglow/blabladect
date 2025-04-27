using Busmail;

class TestProgram
{
    static void Main()
    {
        MessageBus bus = new MessageBus();
        API.API api = new API.API(bus); 

        //fpgcmd.software_Reset();
        api.Hal.Blink(200, 2);
        api.FPGEN.Version();
        api.FPMM.RequestFPID();
    }
}