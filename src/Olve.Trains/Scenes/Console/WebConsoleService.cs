using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Scenes;
using Olve.Paths;
using Olve.Results;
using Olve.Utilities.Collections;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Olve.Trains.Scenes.Console;

public class WebConsoleService(ConsoleSceneProvider consoleSceneProvider, ILoggingManager loggingManager) : SceneService
{
    private WebApplication? _webHost;
    private readonly FixedSizeQueue<LogMessage> _logMessages = new(100);

    public override Result Load()
    {
        loggingManager.OnLog += _logMessages.Enqueue;
        
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddCors(options =>
        
        {
            options.AddPolicy("LocalDev", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.WebHost.UseUrls("http://localhost:5000");
        
        builder.Services.AddTransient<ConsoleCommandService>(_ => consoleSceneProvider.GetService<ConsoleCommandService>());
        builder.Services.AddTransient<ILoggingManager>(_ => consoleSceneProvider.GetService<ILoggingManager>());

        builder.Services.ConfigureHttpJsonOptions(opts =>
        {
            opts.SerializerOptions.Converters.Add(new PathJsonConverter());
        });
        
        _webHost = builder.Build();
        
        _webHost.UseCors("LocalDev");
        
        _webHost.MapGet("/ping", () => HttpResults.Ok());
        _webHost.MapGet("/logs", () => _logMessages.ToArray());
        _webHost.MapGet("/command/{commandString}", (string commandString,
            [FromServices] ConsoleCommandService consoleCommandService,
            [FromServices] ILoggingManager loggingManager) =>
        {
            loggingManager.Log(LogLevel.Debug, $"Executing command: '{commandString}'");
            
            var result = consoleCommandService.Execute(commandString);
            if (result.TryPickProblems(out var problems))
            {
                loggingManager.Log(problems);
                return HttpResults.BadRequest(problems.ToArray());
            }
            
            loggingManager.Log(LogLevel.Debug, $"Command executed successfully: '{commandString}'");

            return HttpResults.Ok();
        });

        _webHost.RunAsync();

        return Result.Success();
    }

    public override Result Unload()
    {
        if (_webHost is null)
        {
            return new ResultProblem("Web host is not running");
        }
        
        loggingManager.OnLog -= _logMessages.Enqueue;
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