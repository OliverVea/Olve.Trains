using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;

namespace Olve.Engine3D.AssetPipeline.Operations;

/// <summary>
///     Compiles shader slang shaders to GLSL
/// </summary>
/// <param name="logger"></param>
public class CompileShaders(ILogger<CompileShaders> logger) : IAsyncOperation<CompileShaders.Request, CompileShaders.Response>
{
    public record Request;
    public record Response;

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogInformation("Compiling shaders with shader slang");

        return new Response();
    }
}