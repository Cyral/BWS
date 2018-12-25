using System;
using Microsoft.AspNetCore.Builder;

namespace BinaryWebSockets {
    /// <summary>
    ///     Provides programmatic configuration for BWS.
    /// </summary>
    public class BWSOptions : WebSocketOptions {
        public BWSOptions() {
            // NGINX closes connections after 60 seconds (by default) if no ping is received from the proxied server
            KeepAliveInterval = TimeSpan.FromSeconds(15);
        }

        public string Path { get; set; }
    }
}