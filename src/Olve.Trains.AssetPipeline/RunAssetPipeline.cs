using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;
using Olve.Trains.AssetPipeline.Assets;
using Olve.Trains.AssetPipeline.Shaders;
using System;
using System.IO;

namespace Olve.Trains.AssetPipeline;

public class RunAssetPipeline(
    ILogger<RunAssetPipeline> logger,
    DownloadAssets downloadAssets,
    ProcessShaders processShaders,
    ProcessAssets processAssets) : IAsyncOperation<RunAssetPipeline.Request>
{
    public record Request(BuildTargets Targets);

    public async Task<Result> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Starting asset pipeline");

        IReadOnlyList<FileInfo> assetFiles = Array.Empty<FileInfo>();

        if (request.Targets.RequiresS3Resources())
        {
            DownloadAssets.Request downloadAssetsRequest = new();
            var downloadAssetsResult = await downloadAssets.ExecuteAsync(downloadAssetsRequest, ct);
            if (downloadAssetsResult.TryPickProblems(out var downloadProblems, out var downloadResponse))
            {
                return downloadProblems.Prepend("Failed to download assets");
            }

            assetFiles = downloadResponse.Files;
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