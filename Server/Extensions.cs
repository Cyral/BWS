using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BinaryWebSockets {
    public static class BinaryWebSocketExtensions {
        public static IApplicationBuilder UseBinaryWebSockets(this IApplicationBuilder app, BWSOptions options) {
            if (app == null) throw new ArgumentNullException(nameof(app));

            app.UseWebSockets(options);

            app.UseMiddleware<BinaryWebSocketMiddleware>(options);

            return app;
        }

        public static IServiceCollection AddBinaryWebSockets(this IServiceCollection services) {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton<NetworkComponent>();

            return services;
        }
    }
}