using System.Net;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace BinaryWebSockets {
    /// <summary>
    /// Represents a WebSocket connection.
    /// </summary>
    public class Connection {
        public long UniqueID { get; }

        public IPAddress RemoteIP { get; }

        public int RemotePort { get; }

        internal WebSocket Socket { get; }
        
        internal CancellationToken CancellationToken { get; }

        internal Connection(long uniqueId, WebSocket socket, ConnectionInfo connectionInfo, CancellationToken ct) {
            UniqueID = uniqueId;
            RemoteIP = connectionInfo.RemoteIpAddress;
            RemotePort = connectionInfo.RemotePort;
            
            Socket = socket;
            CancellationToken = ct;
        }
    }
}