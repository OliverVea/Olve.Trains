using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D;
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

    public static int Main()
    {
        var configuration = new ConfigurationBuilder()
            //.SetBasePath(AppContext.BaseDirectory)
            //.AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        var services = new ServiceCollection();
        services.AddAllServices(configuration);

        var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        var window = Window.Create(WindowOptions);
        var gameManager = serviceProvider.GetRequiredService<GameManager>();
        var result = gameManager.Run(window, SceneIds.MainMenuScene);

        return LogResult(result);
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
}
