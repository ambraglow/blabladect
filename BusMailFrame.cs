namespace Busmail
{
    public struct BusMailFrame
    {
        public byte FrameChar;
        public ushort Length;
        public byte Header;
        public byte[] Mail;
        public byte Checksum;
    }

    public enum FrameType : byte
    {
        Supervisory = 0x80,
        Unnumbered = 0xC0,
        Information = 0x00
    }
    public enum SupervisorId : byte
    {
        ReceiveReady = 0x00,
        Reject = (1<<4),
        ReceiveNotReady = (3<<4)
    }
    public enum Err
    {
        Success,
        InvalidMessage,
        Invalid
    }
}
