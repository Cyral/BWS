using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BinaryWebSockets {
    public class BinaryWebSocketMiddleware {
        private readonly NetworkComponent networkComponent;
        private readonly RequestDelegate next;
        private readonly BWSOptions options;
        private long nextId;

        public BinaryWebSocketMiddleware(BWSOptions options, NetworkComponent networkComponent, RequestDelegate next) {
            this.options = options;
            this.next = next;
            this.networkComponent = networkComponent;
        }

        public async Task InvokeAsync(HttpContext context) {
            // This middleware is only for the specified path.
            if (context.Request.Path != options.Path) {
                await next(context);
                return;
            }

            // A websocket connection (ws:// or wss://) with our "bws" protocol must be used.
            if (!context.WebSockets.IsWebSocketRequest ||
                !context.WebSockets.WebSocketRequestedProtocols.Contains("bws")) {
                context.Response.StatusCode = 400;
                return;
            }

            // Accept the WebSocket connection.
            var socket = await context.WebSockets.AcceptWebSocketAsync("bws");
            var ct = context.RequestAborted;

            // Create a unique ID to identify this connection instance.
            var uniqueId = nextId++;
            var connection = new Connection(uniqueId, socket, context.Connection, ct);

            networkComponent.OnClientConnected(connection);

            // Create a byte array for temporary storage as the data is received, which will be written
            // to a MemoryStream.
            var buffer = new ArraySegment<byte>(new byte[options.ReceiveBufferSize]);

            // Read data until the socket is closed.
            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
                await networkComponent.Read(buffer, connection);

            networkComponent.OnClientDisconnected(connection);
        }
    }
}