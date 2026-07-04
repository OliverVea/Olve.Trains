using Microsoft.Extensions.Logging;
using Olve.Trains.AssetPipeline.Assets;
using Olve.Trains.AssetPipeline.Shaders;
using Olve.Trains.AssetPipeline.Layouts;

namespace Olve.Trains.AssetPipeline;

public class RunAssetPipeline(
    ILogger<RunAssetPipeline> logger,
    PathProvider pathProvider,
    LoadLocalAssets loadLocalAssets,
    ProcessShaders processShaders,
    ProcessLayouts processLayouts,
    ProcessAssets processAssets)
    {
    public record Request(BuildTargets Targets);

    public async Task<Result> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Starting asset pipeline");
        logger.LogInformation("Build folder: {TempFolder}", pathProvider.BuildPath);
        logger.LogInformation("Asset output folder: {OutputFolder}", pathProvider.OutputPath);

        IReadOnlyList<FileInfo> assetFiles = [];

        if (request.Targets.HasFlag(BuildTargets.Layouts))
        {
            ProcessLayouts.Request processLayoutsRequest = new();
            var layoutsResult = await processLayouts.ExecuteAsync(processLayoutsRequest, ct);
            if (layoutsResult.TryPickProblems(out var layoutProblems))
            {
                return layoutProblems.Prepend("Failed to process layouts");
            }
        }

        if (request.Targets.RequiresSourceAssets())
        {
            LoadLocalAssets.Request loadLocalAssetsRequest = new();
            var loadLocalAssetsResult = await loadLocalAssets.ExecuteAsync(loadLocalAssetsRequest, ct);
            if (loadLocalAssetsResult.TryPickProblems(out var loadProblems, out var loadResponse))
            {
                return loadProblems.Prepend("Failed to load source assets");
            }

            assetFiles = loadResponse.Files;
        }

        if (request.Targets.HasFlag(BuildTargets.Shaders))
        {
            ProcessShaders.Request compileShadersRequest = new();
            var compileShadersResult = await processShaders.ExecuteAsync(compileShadersRequest, ct);
            if (compileShadersResult.TryPickProblems(out var shaderProblems))
            {
                return shaderProblems.Prepend("Failed to compile shaders");
            }
        }

        if (request.Targets.RequiresSourceAssets())
        {
            ProcessAssets.Request processAssetsRequest = new(assetFiles, request.Targets);
            var processAssetsResult = await processAssets.ExecuteAsync(processAssetsRequest, ct);
            if (processAssetsResult.TryPickProblems(out var processProblems))
            {
                return processProblems.Prepend("Failed to process assets");
            }
        }

        logger.LogDebug("Asset pipeline completed successfully!");

        return Result.Success();
    }
}
