using Microsoft.Extensions.Logging;
using Olve.Engine3D.Rendering.Entities;
using Olve.Operations;
using Olve.Results;

namespace Olve.Trains.AssetPipeline.Assets;

public class ProcessTextureAssets(ILogger<ProcessTextureAssets> logger, TextureFileReader textureFileReader, AssetWriter assetWriter, TemplateWriter templateWriter) :  IAsyncOperation<ProcessTextureAssets.Request, IReadOnlyList<Asset<TextureData>>>
{
    public record Request(IReadOnlyList<FileInfo> AssetFiles);

    public async Task<Result<IReadOnlyList<Asset<TextureData>>>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Processing texture assets");

        Directory.CreateDirectory(Paths.TextureOutputFolder);

        var texturesResult = textureFileReader.LoadTextures(request.AssetFiles);
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

        var templateOutputPath = Path.Combine(Paths.TextureOutputFolder, "Textures.cs");

        var templateResult = await templateWriter.WriteTemplateAsync("Textures", textureAssets, templateOutputPath, ct);
        if (templateResult.TryPickProblems(out var templateProblems))
        {
            return templateProblems.Prepend("Failed to write template");
        }

        logger.LogDebug("Processed {Count} model(s) successfully!", textureAssets.Count);

        return Result.Success(textureAssets);
    }
}