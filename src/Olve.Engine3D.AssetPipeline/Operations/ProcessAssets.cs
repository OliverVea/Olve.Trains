using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;

namespace Olve.Engine3D.AssetPipeline.Operations;

/// <summary>
///     Processes game assets and spits them out in /app/output.
/// </summary>
public class ProcessAssets(ILogger<ProcessAssets> logger) : IAsyncOperation<ProcessAssets.Request, ProcessAssets.Response>
{
    public record Request(IReadOnlyList<FileInfo> AssetFiles);

    public record Response;

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogInformation("Processing assets");

        return new Response();
    }
}