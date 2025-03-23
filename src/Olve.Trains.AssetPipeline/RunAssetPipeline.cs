using Microsoft.Extensions.Logging;
using Olve.Engine3D.AssetPipeline.Assets;
using Olve.Engine3D.AssetPipeline.Shaders;
using Olve.Operations;
using Olve.Results;

namespace Olve.Engine3D.AssetPipeline;

public class RunAssetPipeline(
    ILogger<RunAssetPipeline> logger,
    DownloadAssets downloadAssets,
    ProcessShaders processShaders,
    ProcessAssets processAssets,
    WriteMetadataSourceFiles writeMetadataSourceFiles) : IAsyncOperation<RunAssetPipeline.Request>
{
    public record Request;

    public async Task<Result> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogInformation("Starting asset pipeline");

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

        WriteMetadataSourceFiles.Request writeMetadataSourceFilesRequest = new(downloadResponse, processResponse, compileResponse);
        var writeMetadataSourceFilesResult = await writeMetadataSourceFiles.ExecuteAsync(writeMetadataSourceFilesRequest, ct);
        if (writeMetadataSourceFilesResult.TryPickProblems(out var writeProblems))
        {
            return writeProblems.Prepend("Failed to write metadata source files");
        }

        logger.LogInformation("Asset pipeline completed successfully!");

        return Result.Success();
    }
}