using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Olve.Trains.AssetPipeline.Assets;
using Olve.Trains.AssetPipeline.Shaders;
using Olve.Trains.AssetPipeline.Layouts;
using Olve.Trains.AssetPipeline.Options;

namespace Olve.Trains.AssetPipeline;

public class RunAssetPipeline(
    ILogger<RunAssetPipeline> logger,
    PathProvider pathProvider,
    DownloadAssets downloadAssets,
    LoadLocalAssets loadLocalAssets,
    ProcessShaders processShaders,
    ProcessLayouts processLayouts,
    ProcessAssets processAssets,
    IOptions<S3Options> s3Options)
    {
    public record Request(BuildTargets Targets, TimeSpan InitialS3Timeout, bool AllowS3Failure);

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

        if (request.Targets.RequiresS3Resources())
        {
            if (s3Options.Value.UseLocalAssets)
            {
                logger.LogInformation("Using local assets from build directory (S3 download skipped)");
                LoadLocalAssets.Request loadLocalAssetsRequest = new();
                var loadLocalAssetsResult = await loadLocalAssets.ExecuteAsync(loadLocalAssetsRequest, ct);
                if (loadLocalAssetsResult.TryPickProblems(out var loadProblems, out var loadResponse))
                {
                    return loadProblems.Prepend("Failed to load local assets");
                }

                assetFiles = loadResponse.Files;
            }
            else
            {
                DownloadAssets.Request downloadAssetsRequest = new(request.InitialS3Timeout, request.AllowS3Failure);
                var downloadAssetsResult = await downloadAssets.ExecuteAsync(downloadAssetsRequest, ct);
                if (downloadAssetsResult.TryPickProblems(out var downloadProblems, out var downloadResponse))
                {
                    if (request.AllowS3Failure)
                    {
                        logger.LogWarning("Failed to download assets: {Problems}", downloadProblems);
                        assetFiles = [];
                    }
                    else
                    {
                        return downloadProblems.Prepend("Failed to download assets");
                    }
                }
                else
                {
                    assetFiles = downloadResponse.Files;
                }
            }
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

        if (request.Targets.RequiresS3Resources())
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
