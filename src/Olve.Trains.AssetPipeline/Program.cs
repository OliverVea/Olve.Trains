using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Olve.Engine3D.Assets.Entities;
using Olve.OpenRaster;
using Olve.Trains.AssetPipeline;
using Olve.Trains.AssetPipeline.Assets;
using Olve.Trains.AssetPipeline.Shaders;
using Olve.Trains.AssetPipeline.Layouts;
using Olve.Trains.AssetPipeline.Fonts;
using Olve.Trains.AssetPipeline.Options;

ServiceCollection serviceCollection = new();

var projectPath = Olve.Paths.Path.TryGetAssemblyExecutable(out var assemblyExecutable)
    ? assemblyExecutable.Parent
    : Olve.Paths.Path.GetCurrentDirectory();

var configurationRoot = new ConfigurationBuilder()
    .SetBasePath(projectPath.Path)
    .AddEnvironmentVariables()
    .AddJsonFile("Properties/appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("Properties/appsettings.local.json", optional: true, reloadOnChange: true)
    .AddUserSecrets(typeof(RunAssetPipeline).Assembly, optional: true, reloadOnChange: true)
    .AddCommandLine(args)
    .Build();

var logLevelString = configurationRoot["Logging:LogLevel:Default"] ?? "Warning";
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
serviceCollection.AddTransient<LoadLocalAssets>();
serviceCollection.AddTransient<PathProvider>();
serviceCollection.AddTransient<NamespaceProvider>();
serviceCollection.AddTransient<ProcessShaders>();
serviceCollection.AddTransient<ProcessLayouts>();
serviceCollection.AddTransient<ProcessFonts>();
serviceCollection.AddTransient<ProcessAssets>();
serviceCollection.AddTransient<ProcessMeshAssets>();
serviceCollection.AddTransient<ProcessTextureAssets>();
serviceCollection.AddTransient<ProcessTerrainAssets>();

serviceCollection.AddTransient<MeshFileReader>();
serviceCollection.AddTransient<TextureFileReader>();
serviceCollection.AddTransient<TerrainFileReader>();

serviceCollection.AddTransient<AssetWriter>();
serviceCollection.AddTransient<TemplateWriter>();

serviceCollection.AddTransient<ILayerParser<HeightmapData>, HeightmapLayerParser>();

serviceCollection.AddTransient<ReadOpenRasterFile>();
serviceCollection.AddTransient<ReadLayerAs<HeightmapData>>();

serviceCollection.AddAssetPipelineConfiguration(configurationRoot);

var serviceProvider = serviceCollection.BuildServiceProvider();

var runAssetPipeline = serviceProvider.GetRequiredService<RunAssetPipeline>();
var logger = serviceProvider.GetRequiredService<ILogger<RunAssetPipeline>>();

logger.LogInformation("--------------------------------------");
logger.LogInformation("|       Starting Asset Pipeline      |");
logger.LogInformation("--------------------------------------");

CancellationTokenSource cts = new();

var buildOptions = serviceProvider.GetRequiredService<IOptions<BuildOptions>>();
var s3Options = serviceProvider.GetRequiredService<IOptions<S3Options>>();

var targets = buildOptions.Value.Targets
    .Select(Enum.Parse<BuildTargets>)
    .Aggregate(BuildTargets.None, (a,b) => a | b);
var timeout = TimeSpan.FromMilliseconds(s3Options.Value.TimeoutMs);

var result = await runAssetPipeline.ExecuteAsync(new(targets, timeout, s3Options.Value.AllowFailure), cts.Token);
if (result.TryPickProblems(out var mainProblems))
{
    foreach (var problem in mainProblems)
    {
        var message = $"{problem.Message} at {{{problem.Args.Length}}}";

        var problemArgs = problem.Args.Append(problem.OriginInformation.LinkString).ToArray();

        logger.LogError(message, problemArgs);
    }

    return 1;
}

logger.LogInformation("--------------------------------------");
logger.LogInformation("|     Asset Pipeline Completed!      |");
logger.LogInformation("--------------------------------------");

return 0;
