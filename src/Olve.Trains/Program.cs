using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
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
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        var listen = parsedArgs.Listen
                     || configuration.GetValue<bool>("Detached:Listen");

        var instanceId = parsedArgs.InstanceId is not null
            ? new GameInstanceId(parsedArgs.InstanceId)
            : new GameInstanceId();

        var services = new ServiceCollection();
        services.AddAllServices(configuration, instanceId, listen);

        var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        // Register global command handlers
        var collection = serviceProvider.GetRequiredService<CommandHandlerServiceCollection>();
        collection.Add(serviceProvider.GetRequiredService<EchoCommandHandler>());
        collection.Add(serviceProvider.GetRequiredService<HelpCommandHandler>());
        collection.Add(serviceProvider.GetRequiredService<ExitCommandHandler>());

        var window = Window.Create(WindowOptions);
        var gameManager = serviceProvider.GetRequiredService<GameManager>();
        var logger = serviceProvider.GetRequiredService<ILogger<GameManager>>();

        if (listen)
        {
            logger.LogInformation("Listening on pipe '{PipeName}'", instanceId.GetPipeName());
        }

        logger.LogInformation("""


                              ----------------------------------------
                              |       Starting On Track to Grow      |
                              ----------------------------------------
                              """);

        var result = gameManager.Run(window, SceneIds.MainMenuScene);

        return LogResult(result);
    }

    private static async Task<int> SendCommandMode(string command, string? instanceId)
    {
        var pipeName = instanceId is not null
            ? new GameInstanceId(instanceId).GetPipeName()
            : GameInstanceId.GetDefaultPipeName();

        var result = await CommandPipeClient.SendCommand(pipeName, command, TimeSpan.FromSeconds(10));

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

    private record ParsedArgs(string? SendCommand, string? InstanceId, bool Listen);

    private static ParsedArgs ParseArguments(string[] args)
    {
        string? sendCommand = null;
        string? instanceId = null;
        var listen = false;

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
            }
        }

        return new ParsedArgs(sendCommand, instanceId, listen);
    }
}
