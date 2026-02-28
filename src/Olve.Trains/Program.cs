using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Input;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Utilities;
using Olve.Trains.Telemetry;
using Silk.NET.Windowing;

namespace Olve.Trains;

public static class Program
{
    private static readonly WindowOptions WindowOptions = WindowOptions.Default with
    {
        Title = "On Track to Grow",
        Size = new Vector2D<int>(1280, 720),
        Samples = 8
    };

    public static async Task<int> Main(string[] args)
    {
        var parsedArgs = ParseArguments(args);

        if (parsedArgs.SendCommand is not null)
        {
            return await SendCommandMode(parsedArgs.SendCommand, parsedArgs.InstanceId);
        }

        return StartGame(parsedArgs);
    }

    private static int StartGame(ParsedArgs parsedArgs)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("Properties/appsettings.json", optional: true)
            .AddJsonFile("Properties/appsettings.local.json", optional: true)
            .Build();

        var listen = parsedArgs.Listen
                     || configuration.GetValue<bool>("Detached:Listen");

        var instanceId = parsedArgs.InstanceId is not null
            ? new GameInstanceId(parsedArgs.InstanceId)
            : new GameInstanceId();

        var manual = parsedArgs.Manual;

        var services = new ServiceCollection();
        services.AddAllServices(configuration, instanceId, listen, manual);

        var windowOptions = parsedArgs.Resolution is { } res
            ? WindowOptions with { Size = res, WindowBorder = WindowBorder.Hidden, WindowState = WindowState.Normal }
            : WindowOptions;
        var window = Window.Create(windowOptions);

        services.AddSingleton(new Provider<IWindow>(window));

        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        // Register global command handlers
        var collection = serviceProvider.GetRequiredService<CommandHandlerServiceCollection>();
        collection.Add(serviceProvider.GetRequiredService<EchoCommandHandler>());
        collection.Add(serviceProvider.GetRequiredService<HelpCommandHandler>());
        collection.Add(serviceProvider.GetRequiredService<ExitCommandHandler>());
        collection.Add(serviceProvider.GetRequiredService<ScreenshotCommandHandler>());
        collection.Add(serviceProvider.GetRequiredService<LoadSceneCommandHandler>());

        if (manual)
        {
            collection.Add(serviceProvider.GetRequiredService<StepCommandHandler>());
            serviceProvider.GetRequiredService<MouseManager>().NormalizedPositionOverride = new(0, 0);
        }

        var gameManager = serviceProvider.GetRequiredService<GameManager>();
        var logger = serviceProvider.GetRequiredService<ILogger<GameManager>>();

        logger.LogInformation("""


                              ----------------------------------------
                              |       Starting On Track to Grow      |
                              ----------------------------------------
                              """);

        if (listen)
        {
            logger.LogInformation("Listening on pipe '{PipeName}'", instanceId.GetPipeName());
        }

        var startScene = parsedArgs.Scene?.ToLowerInvariant() switch
        {
            "game" => SceneIds.GameUIScene,
            _ => SceneIds.MainMenuScene,
        };

        var result = gameManager.Run(startScene);

        MetricsExtensions.ShutdownMetrics();

        return LogResult(result);
    }

    private static async Task<int> SendCommandMode(string command, string? instanceId)
    {
        var pipeName = instanceId is not null
            ? new GameInstanceId(instanceId).GetPipeName()
            : GameInstanceId.GetDefaultPipeName();

        CancellationTokenSource cts = new(TimeSpan.FromSeconds(20));
        var result = await CommandPipeClient.SendCommandAsync(pipeName, command, cts.Token);

        if (result.TryPickProblems(out var problems, out var response))
        {
            foreach (var problem in problems)
            {
                Console.Error.WriteLine(problem.ToDebugString());
            }

            return 1;
        }

        if (response.Success)
        {
            if (!string.IsNullOrEmpty(response.Output))
            {
                Console.WriteLine(response.Output);
            }

            return 0;
        }

        foreach (var error in response.Errors)
        {
            Console.Error.WriteLine(error);
        }

        return 1;
    }

    private static int LogResult(Result result)
    {
        if (result.TryPickProblems(out var problems))
        {
            foreach (var problem in problems)
            {
                Console.WriteLine(problem.ToDebugString());
            }

            return 1;
        }

        Console.WriteLine("Game exited successfully.");
        return 0;
    }

    private record ParsedArgs(string? SendCommand, string? InstanceId, bool Listen, bool Manual, Vector2D<int>? Resolution, string? Scene);

    private static ParsedArgs ParseArguments(string[] args)
    {
        string? sendCommand = null;
        string? instanceId = null;
        var listen = false;
        var manual = false;
        Vector2D<int>? resolution = null;
        string? scene = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--send" when i + 1 < args.Length:
                    sendCommand = args[++i];
                    break;
                case "--instance" when i + 1 < args.Length:
                    instanceId = args[++i];
                    break;
                case "--listen":
                    listen = true;
                    break;
                case "--manual":
                    manual = true;
                    break;
                case "--resolution" when i + 1 < args.Length:
                    var parts = args[++i].Split('x');
                    if (parts.Length == 2
                        && int.TryParse(parts[0], out var w)
                        && int.TryParse(parts[1], out var h))
                    {
                        resolution = new Vector2D<int>(w, h);
                    }
                    break;
                case "--scene" when i + 1 < args.Length:
                    scene = args[++i];
                    break;
            }
        }

        return new ParsedArgs(sendCommand, instanceId, listen, manual, resolution, scene);
    }
}
