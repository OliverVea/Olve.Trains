using Microsoft.Extensions.Logging;
using Olve.Engine3D.Rendering.Entities;
using Olve.Operations;
using Olve.Results;

namespace Olve.Trains.AssetPipeline.Assets;

public class ProcessTerrainAssets(ILogger<ProcessTerrainAssets> logger, NamespaceProvider namespaceProvider, PathProvider pathProvider, TerrainFileReader terrainFileReader, AssetWriter assetWriter, TemplateWriter templateWriter) :  IAsyncOperation<ProcessTerrainAssets.Request, IReadOnlyList<Asset<TerrainData>>>
{
    public record Request(IReadOnlyList<FileInfo> AssetFiles);

    public async Task<Result<IReadOnlyList<Asset<TerrainData>>>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Processing terrain assets");

        pathProvider.TerrainsOutputFolder.EnsurePathExists();

        var terrainsResult = terrainFileReader.LoadAssets(request.AssetFiles);
        if (terrainsResult.TryPickProblems(out var problems, out var terrainAssets))
        {
            return problems.Prepend("Failed to load terrains");
        }

        foreach (var terrainAsset in terrainAssets)
        {
            var writeResult = await assetWriter.WriteAssetAsync(terrainAsset.Data, terrainAsset.Destination, ct);
            if (writeResult.TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to write terrain asset");
            }
        }

        var templateOutputPath = pathProvider.TerrainsOutputFolder / "Terrains.cs";

        var templateResult = await templateWriter.WriteTemplateAsync("Terrains", namespaceProvider.TerrainNamespace, terrainAssets, templateOutputPath, ct);
        if (templateResult.TryPickProblems(out var templateProblems))
        {
            return templateProblems.Prepend("Failed to write template");
        }

        logger.LogDebug("Processed {Count} terrain(s) successfully!", terrainAssets.Count);

        return Result.Success(terrainAssets);
    }
}