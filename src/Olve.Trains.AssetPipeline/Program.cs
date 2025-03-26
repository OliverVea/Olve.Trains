using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Olve.Engine3D.Rendering.Entities;
using Olve.OpenRaster;
using Olve.Trains.AssetPipeline;
using Olve.Trains.AssetPipeline.Assets;
using Olve.Trains.AssetPipeline.Shaders;

ServiceCollection serviceCollection = new();

var logLevelString = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
var logLevel = Enum.Parse<LogLevel>(logLevelString);

serviceCollection.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddConsole(options =>
    {
        options.FormatterName = CustomFormatter.FormatterName;
    });

    builder.AddConsoleFormatter<CustomFormatter, ConsoleFormatterOptions>();
    builder.SetMinimumLevel(logLevel);
});

serviceCollection.AddTransient<RunAssetPipeline>();
serviceCollection.AddTransient<DownloadAssets>();
serviceCollection.AddTransient<ProcessShaders>();
serviceCollection.AddTransient<ProcessAssets>();
serviceCollection.AddTransient<ProcessMeshAssets>();
serviceCollection.AddTransient<ProcessTextureAssets>();
serviceCollection.AddTransient<ProcessTerrainAssets>();
serviceCollection.AddTransient<WriteMetadataSourceFiles>();

serviceCollection.AddTransient<MeshFileReader>();
serviceCollection.AddTransient<TextureFileReader>();
serviceCollection.AddTransient<TerrainFileReader>();

serviceCollection.AddTransient<AssetWriter>();
serviceCollection.AddTransient<TemplateWriter>();

serviceCollection.AddTransient<ReadOpenRasterFile>();
serviceCollection.AddTransient<ReadLayerAs<HeightmapData>>();

ILayerParser<HeightmapData> heightmapLayerParser = new HeightmapLayerParser(0.25f, 128, 8);
serviceCollection.AddSingleton(heightmapLayerParser);



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