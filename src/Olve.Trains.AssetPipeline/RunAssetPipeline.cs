using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;
using Olve.Trains.AssetPipeline.Assets;
using Olve.Trains.AssetPipeline.Shaders;

namespace Olve.Trains.AssetPipeline;

public class RunAssetPipeline(
    ILogger<RunAssetPipeline> logger,
    DownloadAssets downloadAssets,
    ProcessShaders processShaders,
    ProcessAssets processAssets) : IAsyncOperation<RunAssetPipeline.Request>
{
    public record Request;

    public async Task<Result> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Starting asset pipeline");

        DownloadAssets.Request downloadAssetsRequest = new();
        var downloadAssetsResult = await downloadAssets.ExecuteAsync(downloadAssetsRequest, ct);
        if (downloadAssetsResult.TryPickProblems(out var downloadProblems, out var downloadResponse))
        {
            return downloadProblems.Prepend("Failed to download assets");
        }

        ProcessShaders.Request compileShadersRequest = new();
        var compileShadersResult = await processShaders.ExecuteAsync(compileShadersRequest, ct);
        if (compileShadersResult.TryPickProblems(out var shaderProblems, out var compileResponse))
        {
            return shaderProblems.Prepend("Failed to compile shaders");
        }

        ProcessAssets.Request processAssetsRequest = new(downloadResponse.Files);
        var processAssetsResult = await processAssets.ExecuteAsync(processAssetsRequest, ct);
        if (processAssetsResult.TryPickProblems(out var processProblems, out var processResponse))
        {
            return processProblems.Prepend("Failed to process assets");
        }

        logger.LogDebug("Asset pipeline completed successfully!");

        return Result.Success();
    }
}