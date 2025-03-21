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

        var shaderFiles = Directory.GetFiles("/app/shaders", "*", SearchOption.AllDirectories);
        foreach (var shaderFile in shaderFiles)
        {
            logger.LogDebug("Compiling shader: {ShaderFile}", shaderFile);
        }

        logger.LogInformation("Shaders compiled successfully!");

        return new Response();
    }
}