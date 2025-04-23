namespace Busmail {
    public class Buffer {
        public byte[]? Read;
        public byte[]? Write;
    }
    public class Data {
        public BusMailFrame SavedFrame;
        public BusMailFrame IncomingframeData;
        private int _lost = 0;
        public int Lost {
            get => _lost;
            set {
                if (value >=7 ) _lost = 0;
                else _lost = value;
            }
        }
        public bool PollFinal = false;
        public bool IncomingPollFinal = false;
    }
}