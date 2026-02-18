using Microsoft.Extensions.Logging;
using Olve.Engine3D.Assets.Entities;
using Olve.Operations;
using Olve.Paths;
using Olve.Results;

namespace Olve.Trains.AssetPipeline.Assets;

public class ProcessMeshAssets(ILogger<ProcessMeshAssets> logger, NamespaceProvider namespaceProvider, PathProvider pathProvider, MeshFileReader meshFileReader, AssetWriter assetWriter, TemplateWriter templateWriter) :  IAsyncOperation<ProcessMeshAssets.Request, IReadOnlyList<Asset<MeshData>>>
{
    public record Request(IReadOnlyList<FileInfo> AssetFiles);

    public async Task<Result<IReadOnlyList<Asset<MeshData>>>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Processing mesh assets");

        pathProvider.MeshesOutputFolder.EnsurePathExists();

        var meshesResult = meshFileReader.LoadMeshes(request.AssetFiles);
        if (meshesResult.TryPickProblems(out var problems, out var meshAssets))
        {
            return problems.Prepend("Failed to load meshes");
        }

        foreach (var meshAsset in meshAssets)
        {
            var writeResult = await assetWriter.WriteAssetAsync(meshAsset.Data, meshAsset.Destination, ct);
            if (writeResult.TryPickProblems(out var writeProblems))
            {
                return writeProblems.Prepend("Failed to write mesh asset");
            }
        }

        var templateOutputPath = pathProvider.MeshesOutputFolder / "Meshes.cs";

        var templateResult = await templateWriter.WriteTemplateAsync("Meshes", namespaceProvider.MeshNamespace, "MeshData", meshAssets, templateOutputPath, ct);
        if (templateResult.TryPickProblems(out var templateProblems))
        {
            return templateProblems.Prepend("Failed to write template");
        }

        logger.LogDebug("Processed {Count} mesh(es) successfully!", meshAssets.Count);

        return Result.Success(meshAssets);
    }
}