using Microsoft.Extensions.Logging;
using Olve.Engine3D.Rendering.Entities;
using Olve.Operations;
using Olve.Results;

namespace Olve.Trains.AssetPipeline.Assets;

/// <summary>
///     Processes game assets and spits them out in /app/output.
/// </summary>
public class ProcessAssets(ILogger<ProcessAssets> logger, ProcessMeshAssets processMeshAssets, ProcessTextureAssets processTextureAssets, ProcessTerrainAssets processTerrainAssets) : IAsyncOperation<ProcessAssets.Request, ProcessAssets.Response>
{
    private static readonly string TemplateFilePath = Path.Combine(Paths.TemplatesSourceFolder, "MeshesClass.scriban");

    public record Request(IReadOnlyList<FileInfo> AssetFiles, BuildTargets Targets);
    public record Response(IReadOnlyList<Asset<MeshData>> MeshAssets, IReadOnlyList<Asset<TextureData>> TextureAssets, IReadOnlyList<Asset<TerrainData>> TerrainAssets);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Processing assets");

        IReadOnlyList<Asset<MeshData>> meshAssets = [];
        IReadOnlyList<Asset<TextureData>> textureAssets = [];
        IReadOnlyList<Asset<TerrainData>> terrainAssets = [];

        if (request.Targets.HasFlag(BuildTargets.Meshes))
        {
            ProcessMeshAssets.Request meshRequest = new(request.AssetFiles);
            var meshResponse = await processMeshAssets.ExecuteAsync(meshRequest, ct);
            if (meshResponse.TryPickProblems(out var meshProblems, out var meshes))
            {
                return meshProblems.Prepend("Failed to process mesh assets");
            }

            meshAssets = meshes;
        }

        if (request.Targets.HasFlag(BuildTargets.Textures))
        {
            ProcessTextureAssets.Request textureRequest = new(request.AssetFiles);
            var textureResponse = await processTextureAssets.ExecuteAsync(textureRequest, ct);
            if (textureResponse.TryPickProblems(out var textureProblems, out var textures))
            {
                return textureProblems.Prepend("Failed to process texture assets");
            }

            textureAssets = textures;
        }

        if (request.Targets.HasFlag(BuildTargets.Terrains))
        {
            ProcessTerrainAssets.Request terrainRequest = new(request.AssetFiles);
            var terrainResponse = await processTerrainAssets.ExecuteAsync(terrainRequest, ct);
            if (terrainResponse.TryPickProblems(out var terrainProblems, out var terrains))
            {
                return terrainProblems.Prepend("Failed to process terrain assets");
            }

            terrainAssets = terrains;
        }

        logger.LogInformation(
            "Processed {MeshCount} mesh(es), {TextureCount} texture(s), and {TerrainCount} terrain(s) successfully!",
            meshAssets.Count,
            textureAssets.Count,
            terrainAssets.Count);

        return new Response(meshAssets, textureAssets, terrainAssets);
    }
}