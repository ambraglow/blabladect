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
        Reject = 0x08,
        ReceiveNotReady = 0x18
    }

    public enum Err
    {
        Success,
        InvalidMessage
    }
}
