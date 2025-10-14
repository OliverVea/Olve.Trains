using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Configuration;
using Olve.Engine3D.Rendering.Entities;
using Olve.OpenRaster;
using Olve.Trains.AssetPipeline;
using Olve.Trains.AssetPipeline.Assets;
using Olve.Trains.AssetPipeline.Shaders;
using Olve.Trains.AssetPipeline.Layouts;

ServiceCollection serviceCollection = new();

var configurationRoot = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
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
serviceCollection.AddTransient<ProcessShaders>();
serviceCollection.AddTransient<ProcessLayouts>();
serviceCollection.AddTransient<ProcessAssets>();
serviceCollection.AddTransient<ProcessMeshAssets>();
serviceCollection.AddTransient<ProcessTextureAssets>();
serviceCollection.AddTransient<ProcessTerrainAssets>();

serviceCollection.AddTransient<MeshFileReader>();
serviceCollection.AddTransient<TextureFileReader>();
serviceCollection.AddTransient<TerrainFileReader>();

serviceCollection.AddTransient<AssetWriter>();
serviceCollection.AddTransient<TemplateWriter>();

serviceCollection.AddTransient<ReadOpenRasterFile>();
serviceCollection.AddTransient<ReadLayerAs<HeightmapData>>();

ILayerParser<HeightmapData> heightmapLayerParser = new HeightmapLayerParser(0.25f, 128, 8);
serviceCollection.AddSingleton(heightmapLayerParser);

var shadersSection = configurationRoot.GetSection("Shaders");
var shaderOptions = new ShaderOptions();
shadersSection.Bind(shaderOptions);
serviceCollection.AddSingleton(shaderOptions);

var layoutsSection = configurationRoot.GetSection("Layouts");
var layoutOptions = new LayoutOptions();
layoutsSection.Bind(layoutOptions);
serviceCollection.AddSingleton(layoutOptions);

var s3Section = configurationRoot.GetSection("S3");
var s3Options = new S3Options();
s3Section.Bind(s3Options);
serviceCollection.AddSingleton(s3Options);

var serviceProvider = serviceCollection.BuildServiceProvider();

var runAssetPipeline = serviceProvider.GetRequiredService<RunAssetPipeline>();
var logger = serviceProvider.GetRequiredService<ILogger<RunAssetPipeline>>();

logger.LogInformation("--------------------------------------");
logger.LogInformation("|       Starting Asset Pipeline      |");
logger.LogInformation("--------------------------------------");

CancellationTokenSource cts = new();

// Resolve build targets from configuration (with legacy fallback)
BuildTargets targets;
var targetsConfig = configurationRoot["Build:Targets"];
if (!string.IsNullOrWhiteSpace(targetsConfig))
{
    var tokens = targetsConfig
        .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
        .Select(t => "--" + t.Trim().TrimStart('-'))
        .ToArray();
    targets = BuildTargetParser.Parse(tokens);
}
else
{
    targets = BuildTargetParser.Parse(args);
}

// S3 timeout and failure mode from configuration
var initialTimeout = TimeSpan.FromMilliseconds(s3Options.TimeoutMs > 0 ? s3Options.TimeoutMs : 20000);
var allowS3Failure = s3Options.AllowFailure;

var result = await runAssetPipeline.ExecuteAsync(new(targets, initialTimeout, allowS3Failure), cts.Token);
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
