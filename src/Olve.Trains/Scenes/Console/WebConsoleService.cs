using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.DebugServer;
using Olve.Engine3D.DebugServer.Commands;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Paths;

namespace Olve.Trains.Scenes.Console;

public class WebConsoleService(ILoggingManager loggingManager, ConsoleSceneProvider consoleSceneProvider) : SceneService(loggingManager)
{
    private WebApplication? _webHost;

    private const bool AllowAnyCors = true;

    protected override Result OnLoad()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddTransient<IEnumerable<ICommandHandler>>(sp => consoleSceneProvider.GetRequiredService<CommandHandlerServiceCollection>().Concat([sp.GetRequiredService<HelpCommandHandler>()]));
        
        builder.Services.AddOlveEngine3DDebugServer<InMemoryLoggingManager>();
        builder.Services.AddSingleton(LoggingManager);
        
        if (AllowAnyCors) builder.Services.AddCors(c => c.AddDefaultPolicy(p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
        
        _webHost = builder.Build();

        if (AllowAnyCors)
        {
            LoggingManager.Log(LogLevel.Warning, "AllowAnyCors is enabled");
            _webHost.UseCors();
        }

        _webHost.ConfigureOlveEngine3DDebugServer();

        _webHost.RunAsync();

        return Result.Success();
    }

    protected override Result OnUnload()
    {
        if (_webHost is null)
        {
            return new ResultProblem("Web host is not running");
        }
        
        _webHost?.StopAsync().GetAwaiter().GetResult();
        return Result.Success();
    }
}

public class PathJsonConverter : JsonConverter<IPath>
{
    public override IPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Paths.Path.Create(reader.GetString() ?? string.Empty);

    public override void Write(Utf8JsonWriter writer, IPath value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Path);
}