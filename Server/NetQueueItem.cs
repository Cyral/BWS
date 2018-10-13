namespace BinaryWebSockets {
    internal class NetQueueItem {
        public Connection Connection { get; set; } 
        public IncomingMessageType Type { get; set; }
        public IncomingMessage Message { get; set; }

        internal NetQueueItem(Connection connection, IncomingMessageType type, IncomingMessage incomingMessage = null) {
            Connection = connection;
            Type = type;
            Message = incomingMessage;
        }
    }
}