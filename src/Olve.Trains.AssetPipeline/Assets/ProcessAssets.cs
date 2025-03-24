using Microsoft.Extensions.Logging;
using Olve.Engine3D.Rendering.Entities;
using Olve.Operations;
using Olve.Results;

namespace Olve.Trains.AssetPipeline.Assets;

/// <summary>
///     Processes game assets and spits them out in /app/output.
/// </summary>
public class ProcessAssets(ILogger<ProcessAssets> logger, ProcessMeshAssets processMeshAssets, ProcessTextureAssets processTextureAssets) : IAsyncOperation<ProcessAssets.Request, ProcessAssets.Response>
{
    private static readonly string TemplateFilePath = Path.Combine(Paths.TemplatesSourceFolder, "MeshesClass.scriban");

    public record Request(IReadOnlyList<FileInfo> AssetFiles);
    public record Response(IReadOnlyList<Asset<MeshData>> MeshAssets, IReadOnlyList<Asset<TextureData>> TextureAssets);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogInformation("Processing assets");

        var meshRequest = new ProcessMeshAssets.Request(request.AssetFiles);
        var meshResponse = await processMeshAssets.ExecuteAsync(meshRequest, ct);
        if (meshResponse.TryPickProblems(out var meshProblems, out var meshAssets))
        {
            return meshProblems.Prepend("Failed to process mesh assets");
        }

        var textureRequest = new ProcessTextureAssets.Request(request.AssetFiles);
        var textureResponse = await processTextureAssets.ExecuteAsync(textureRequest, ct);
        if (textureResponse.TryPickProblems(out var textureProblems, out var textureAssets))
        {
            return textureProblems.Prepend("Failed to process texture assets");
        }

        logger.LogInformation("Processed {MeshCount} mesh(es) and {TextureCount} texture(s) successfully!", meshAssets.Count, textureAssets.Count);

        return new Response(meshAssets, textureAssets);
    }
}