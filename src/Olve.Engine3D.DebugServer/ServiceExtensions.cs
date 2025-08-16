using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.DebugServer.Commands;
using Olve.Logging;
using Olve.MinimalApi;

namespace Olve.Engine3D.DebugServer;

public static class ServiceExtensions
{
    public static IServiceCollection AddOlveEngine3DDebugServer<TLoggingManager>(this IServiceCollection services) where TLoggingManager : class, ILoggingManager
    {
        services.AddSingleton<ILoggingManager, TLoggingManager>();
        services.AddSingleton<IHandler<GetLogsRequest, GetLogsResponse>, LoggingHandler>();
        
        services.AddSingleton<CommandRunner>();
        services.AddSingleton<IHandler<RunCommandRequest>>(sp => sp.GetRequiredService<CommandRunner>());
        services.AddSingleton<ICommandRunner>(sp => sp.GetRequiredService<CommandRunner>());

        services.AddSingleton<HelpCommandHandler>();
        services.AddSingleton<ICommandHandler>(sp => sp.GetRequiredService<HelpCommandHandler>());
        services.AddSingleton<ICommandHandler, EchoCommandHandler>();
        services.AddSingleton<ICommandHandler>(sp => sp.GetRequiredService<EchoCommandHandler>());
        
        services.WithPathJsonConversion();
        services.ConfigureHttpJsonOptions(json =>
        {
            json.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        
        services.AddSingleton<WebSocketManager>();
        services.AddHostedService<StartupTask>();
        services.AddTransient<LogsUpdatedEvent>(sp => sp.GetRequiredService<ILoggingManager>().LogsUpdatedEvent);

        services.AddOpenApi();
        
        return services;
    }
}