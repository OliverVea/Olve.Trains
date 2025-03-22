using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Olve.Engine3D.AssetPipeline;
using Olve.Engine3D.AssetPipeline.Operations;

ServiceCollection serviceCollection = new();

var logLevelString = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
var logLevel = Enum.Parse<LogLevel>(logLevelString);

serviceCollection.AddLogging(builder =>
{
    builder.ClearProviders(); // Remove default providers
    builder.AddConsole(options =>
    {
        options.FormatterName = "custom";
    });

    builder.AddConsoleFormatter<CustomFormatter, ConsoleFormatterOptions>();
    builder.SetMinimumLevel(logLevel);
});

serviceCollection.AddTransient<RunAssetPipeline>();
serviceCollection.AddTransient<DownloadAssets>();
serviceCollection.AddTransient<ProcessShaders>();
serviceCollection.AddTransient<ProcessAssets>();
serviceCollection.AddTransient<WriteMetadataSourceFiles>();

var serviceProvider = serviceCollection.BuildServiceProvider();

var runAssetPipeline = serviceProvider.GetRequiredService<RunAssetPipeline>();
var logger = serviceProvider.GetRequiredService<ILogger<RunAssetPipeline>>();

logger.LogInformation("--------------------------------------");
logger.LogInformation("|       Starting Asset Pipeline      |");
logger.LogInformation("--------------------------------------");

CancellationTokenSource cts = new();

var result = await runAssetPipeline.ExecuteAsync(new (), cts.Token);
if (result.TryPickProblems(out var mainProblems))
{
    foreach (var problem in mainProblems)
    {
        logger.LogError(problem.ToDebugString());
    }

    return 1;
}

logger.LogInformation("--------------------------------------");
logger.LogInformation("|     Asset Pipeline Completed!      |");
logger.LogInformation("--------------------------------------");


return 0;