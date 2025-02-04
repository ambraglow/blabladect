using System;
using Busmail;
using API;

class Program
{
    static ReadOnlySpan<byte> FrameData => [ 0x40, 0x05 ];

    static void Main()
    {
        MessageBus bus = new MessageBus();
        MessageBusHandle.InitializeConnection(bus);

        //var infoFrame = FrameBuilder.BuildFrame(FrameType.Information, FrameData.ToArray(), true);
        //FrameBuilder.FrameToData(infoFrame);

        /*
        var infoFrameTwo = FrameBuilder.BuildFrame(FrameType.Information, FrameData.ToArray(), true);
        byte[] reconstructedTwo = FrameBuilder.FrameToData(infoFrameTwo);

        var SupervisoryFrame = FrameBuilder.BuildFrame(FrameType.Supervisory, null, false, SupervisorId.ReceiveReady);
        byte[] reconstructedSupervisory = FrameBuilder.FrameToData(SupervisoryFrame);

        var SABM = FrameBuilder.BuildFrame(FrameType.Unnumbered, null, true);
        byte[] reconstructedSABM = FrameBuilder.FrameToData(SABM);
        */

    }
}