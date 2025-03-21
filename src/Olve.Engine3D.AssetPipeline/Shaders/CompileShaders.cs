using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;

namespace Olve.Engine3D.AssetPipeline.Shaders;

/// <summary>
///     Compiles shader slang shaders to GLSL
/// </summary>
/// <param name="logger"></param>
public class CompileShaders(ILogger<CompileShaders> logger) : IAsyncOperation<CompileShaders.Request>
{
    public record Request;

    public async Task<Result> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogInformation("Compiling shaders with shader slang");

        return Result.Success();
    }
}