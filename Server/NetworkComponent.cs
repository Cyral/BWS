using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryWebSockets {
    public class NetworkComponent {
        public delegate void MessageReceivedEvent(Connection connection, IncomingMessage message);

        public delegate void ClientConnectedEvent(Connection connection);

        public delegate void ClientDisconnectedEvent(Connection connection);

        /// <summary>
        /// Event when a message is received from a client.
        /// </summary>
        public event MessageReceivedEvent MessageReceived;
        
        /// <summary>
        /// Event when a client is connected.
        /// </summary>
        public event ClientConnectedEvent ClientConnected;
        
        /// <summary>
        /// Event when a client is disconnected.
        /// </summary>
        public event ClientDisconnectedEvent ClientDisconnected;

        /// <summary>
        /// Triggered when an event to be ran on the network thread is received.
        /// </summary>
        private readonly AutoResetEvent eventTrigger;

        /// <summary>
        /// Events to be ran on the network thread.
        /// </summary>
        private readonly ConcurrentQueue<NetQueueItem> eventQueue;

        public NetworkComponent() {
            eventQueue = new ConcurrentQueue<NetQueueItem>();
            eventTrigger = new AutoResetEvent(false);

            var networkThread = new Thread(ProcessNetworkEvents);
            networkThread.Start();
        }

        /// <summary>
        /// Creates a new outgoing message.
        /// </summary>
        public OutgoingMessage CreateMessage() {
            return new OutgoingMessage();
        }

        /// <summary>
        /// Disposes an incoming message.
        /// </summary>
        public void Recycle(IncomingMessage inc) {
            inc.Dispose();
        }

        /// <summary>
        /// Sends a message to the specified connection.
        /// </summary>
        public async Task Send(OutgoingMessage message, Connection connection) {
            if (connection.CancellationToken.IsCancellationRequested) return;
            var data = message.GetData();
            await connection.Socket.SendAsync(data, WebSocketMessageType.Binary, true, connection.CancellationToken);
        }

        /// <summary>
        /// Closes a client connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task Close(Connection connection, WebSocketCloseStatus closeStatus, string statusDescription) {
            await connection.Socket.CloseAsync(closeStatus, statusDescription, connection.CancellationToken);
        }

        internal void OnClientConnected(Connection connection) {
            eventQueue.Enqueue(new NetQueueItem(connection, IncomingMessageType.Connect));
            eventTrigger.Set();
        }

        internal void OnClientDisconnected(Connection connection) {
            eventQueue.Enqueue(new NetQueueItem(connection, IncomingMessageType.Disconnect));
            eventTrigger.Set();
        }

        /// <summary>
        /// Reads a message from the WebSocket.
        /// </summary>
        internal async Task Read(ArraySegment<byte> buffer, Connection connection) {
            var ms = new MemoryStream();
            WebSocketReceiveResult result;
            
            // Wait for data, read all of it, and write it to the memory stream.
            do {
                try {
                    result = await connection.Socket.ReceiveAsync(buffer, connection.CancellationToken);
                }
                catch (OperationCanceledException) {
                    // If the socket is closed while reading.
                    ms.Dispose();
                    return;
                }
                
                ms.Write(buffer.Array, buffer.Offset, result.Count);
            } while (!result.EndOfMessage && !connection.CancellationToken.IsCancellationRequested);

            // Do not allow text data to be sent.
            if (result.MessageType == WebSocketMessageType.Text) {
                await Close(connection, WebSocketCloseStatus.PolicyViolation, "Binary data only");
                ms.Dispose();
                return;
            }

            // If the socket has been closed, we don't need to construct a data message, just return.
            if (connection.CancellationToken.IsCancellationRequested || result.MessageType == WebSocketMessageType.Close) {
                ms.Dispose();
                return;
            }

            ms.Seek(0, SeekOrigin.Begin);

            var binaryReader = new BinaryReader(ms, Encoding.UTF8);

            // Create a new incoming message from the received bytes.
            var inc = new IncomingMessage(binaryReader, connection);

            eventQueue.Enqueue(new NetQueueItem(connection, IncomingMessageType.Data, inc));
            eventTrigger.Set();
        }

        /// <summary>
        /// Waits for new events from another thread and then processes the event queue when messages
        /// are available. This serves to run all message events on a single thread instead of the multiple threads
        /// that ASP.NET uses for handling requests.
        /// </summary>
        private void ProcessNetworkEvents() {
            while (true) {
                // Wait until a new message is received.
                eventTrigger.WaitOne();

                // Dequeue all received messages and execute the appropriate event.
                while (!eventQueue.IsEmpty) {
                    if (eventQueue.TryDequeue(out var item)) {
                        switch (item.Type) {
                            case IncomingMessageType.Connect:
                                ClientConnected?.Invoke(item.Connection);
                                break;
                            case IncomingMessageType.Disconnect:
                                ClientDisconnected?.Invoke(item.Connection);
                                break;
                            case IncomingMessageType.Data:
                                MessageReceived?.Invoke(item.Connection, item.Message);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }
    }
}