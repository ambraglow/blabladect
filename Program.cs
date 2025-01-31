using System;
using Busmail;


class Program
{
    static ReadOnlySpan<byte> FrameData => [ 0x40, 0x05 ];

    static void Main()
    {
        var infoFrame = FrameBuilder.BuildFrame(FrameType.Information, FrameData.ToArray(), true);
        byte[] reconstructed = FrameBuilder.FrameToData(infoFrame);

        var infoFrameTwo = FrameBuilder.BuildFrame(FrameType.Information, FrameData.ToArray(), true);
        byte[] reconstructedTwo = FrameBuilder.FrameToData(infoFrameTwo);

        Console.WriteLine(BitConverter.ToString(reconstructed).Replace("-", " "));
        Console.WriteLine(BitConverter.ToString(reconstructedTwo).Replace("-", " "));
    }
}