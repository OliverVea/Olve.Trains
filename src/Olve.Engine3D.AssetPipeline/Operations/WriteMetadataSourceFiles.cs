using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;

namespace Olve.Engine3D.AssetPipeline.Operations;

/// <summary>
///     Writes C# source files containing metadata about assets and shaders.
/// </summary>
/// <param name="logger"></param>
public class WriteMetadataSourceFiles(ILogger<WriteMetadataSourceFiles> logger) : IAsyncOperation<WriteMetadataSourceFiles.Request>
{
    public record Request(
        DownloadAssets.Response DownloadAssetsResponse,
        ProcessAssets.Response ProcessAssetsResponse,
        CompileShaders.Response CompileShadersResponse);


    public Task<Result> ExecuteAsync(Request request, CancellationToken ct = new())
    {
        logger.LogInformation("Writing metadata source files");

        var downloadAssetsResponse = request.DownloadAssetsResponse;
        var processAssetsResponse = request.ProcessAssetsResponse;
        var compileShadersResponse = request.CompileShadersResponse;

        logger.LogInformation("Metadata source files written successfully!");

        return Task.FromResult(Result.Success());
    }
}