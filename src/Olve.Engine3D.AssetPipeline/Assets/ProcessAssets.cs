using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;

namespace Olve.Engine3D.AssetPipeline.Assets;

/// <summary>
///     Processes game assets and spits them out in /app/output.
/// </summary>
public class ProcessAssets(ILogger<ProcessAssets> logger) : IAsyncOperation<ProcessAssets.Request>
{
    public record Request;

    public async Task<Result> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogInformation("Processing assets");

        return Result.Success();
    }
}