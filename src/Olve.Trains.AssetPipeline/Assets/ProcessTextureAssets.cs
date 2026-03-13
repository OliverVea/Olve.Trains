using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets.Entities;

namespace Olve.Trains.AssetPipeline.Assets;

public class ProcessTextureAssets(
    ILogger<ProcessTextureAssets> logger,
    NamespaceProvider namespaceProvider,
    PathProvider pathProvider,
    TextureFileReader textureFileReader,
    AssetWriter assetWriter,
    TemplateWriter templateWriter)
{
    public record Request(IReadOnlyList<FileInfo> AssetFiles);

    public async Task<Result<IReadOnlyList<Asset<TextureData<RGBA>>>>> ExecuteAsync(Request request,
        CancellationToken ct = default)
    {
        logger.LogDebug("Processing texture assets");

        pathProvider.TexturesOutputFolder.EnsurePathExists();

        // Exclude PNGs that have a companion .json (those are texture atlas assets)
        var jsonNames = request.AssetFiles
            .Where(f => f.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            .Select(f => System.IO.Path.GetFileNameWithoutExtension(f.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var standaloneTextures = request.AssetFiles
            .Where(f => !f.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                        || !jsonNames.Contains(System.IO.Path.GetFileNameWithoutExtension(f.Name)))
            .ToList();

        var texturesResult = textureFileReader.LoadTextures(standaloneTextures);
        if (texturesResult.TryPickProblems(out var problems, out var textureAssets))
        {
            return problems.Prepend("Failed to load textures");
        }

        foreach (var textureAsset in textureAssets)
        {
            var writeResult = await assetWriter.WriteAssetAsync(textureAsset.Data, textureAsset.Destination, ct);
            if (writeResult.TryPickProblems(out var writeProblems))
            {
                return writeProblems.Prepend("Failed to write texture asset");
            }
        }

        var templateOutputPath = pathProvider.TexturesOutputFolder / "Textures.cs";

        var templateResult = await templateWriter.WriteTemplateAsync("Textures",
            namespaceProvider.TextureNamespace,
            "TextureData<RGBA>",
            textureAssets,
            templateOutputPath,
            ct);
        if (templateResult.TryPickProblems(out var templateProblems))
        {
            return templateProblems.Prepend("Failed to write template");
        }

        logger.LogDebug("Processed {Count} model(s) successfully!", textureAssets.Count);

        return Result.Success(textureAssets);
    }
}