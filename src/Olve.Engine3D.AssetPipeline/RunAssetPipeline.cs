using Microsoft.Extensions.Logging;
using Olve.Engine3D.AssetPipeline.Assets;
using Olve.Engine3D.AssetPipeline.S3;
using Olve.Engine3D.AssetPipeline.Shaders;
using Olve.Operations;
using Olve.Results;

namespace Olve.Engine3D.AssetPipeline;

public class RunAssetPipeline(ILogger<RunAssetPipeline> logger, DownloadAssetsToTemp downloadAssetsToTemp, CompileShaders compileShaders, ProcessAssets processAssets) : IAsyncOperation<RunAssetPipeline.Request>
{
    public record Request;

    public async Task<Result> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogInformation("Starting asset pipeline");

        var downloadAssetsResult = await downloadAssetsToTemp.ExecuteAsync(new(), ct);
        if (downloadAssetsResult.TryPickProblems(out var downloadProblems))
        {
            return downloadProblems.Prepend("Failed to download assets");
        }

        var compileShadersResult = await compileShaders.ExecuteAsync(new(), ct);
        if (compileShadersResult.TryPickProblems(out var shaderProblems))
        {
            return shaderProblems.Prepend("Failed to compile shaders");
        }

        var processAssetsResult = await processAssets.ExecuteAsync(new(), ct);
        if (processAssetsResult.TryPickProblems(out var processProblems))
        {
            return processProblems.Prepend("Failed to process assets");
        }

        logger.LogInformation("Asset pipeline completed successfully!");

        return Result.Success();
    }
}