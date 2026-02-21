using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets.Entities;

namespace Olve.Trains.AssetPipeline.Assets;

/// <summary>
///     Processes game assets and spits them out in /app/output.
/// </summary>
public class ProcessAssets(
    ILogger<ProcessAssets> logger,
    ProcessMeshAssets processMeshAssets,
    ProcessTextureAssets processTextureAssets,
    ProcessTerrainAssets processTerrainAssets,
    Fonts.ProcessFonts processFonts,
    TextureFileReader textureFileReader,
    AssetWriter assetWriter)
{
    public record Request(IReadOnlyList<FileInfo> AssetFiles, BuildTargets Targets);
    public record Response(IReadOnlyList<Asset<MeshData>> MeshAssets,
        IReadOnlyList<Asset<TextureData<RGBA>>> TextureAssets,
        IReadOnlyList<Asset<TerrainData>> TerrainAssets,
        IReadOnlyList<IPath> FontFiles);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Processing assets");

        IReadOnlyList<Asset<MeshData>> meshAssets = [];
        IReadOnlyList<Asset<TextureData<RGBA>>> textureAssets = [];
        IReadOnlyList<Asset<TerrainData>> terrainAssets = [];
        IReadOnlyList<IPath> fontFiles = [];

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

        if (request.Targets.HasFlag(BuildTargets.Fonts))
        {
            Fonts.ProcessFonts.Request fontRequest = new(request.AssetFiles);
            var fontResponse = await processFonts.ExecuteAsync(fontRequest, ct);
            if (fontResponse.TryPickProblems(out var fontProblems, out var fonts))
            {
                return fontProblems.Prepend("Failed to process font assets");
            }

            fontFiles = fonts.GeneratedFiles;

            // Process font atlases as textures and output to fonts folder
            foreach (var fontAtlas in fonts.FontAtlases)
            {
                var atlasFile = new FileInfo(fontAtlas.Atlas.Absolute.Path);
                if (!atlasFile.Exists)
                {
                    return new ResultProblem("Font atlas file not found: {0}", fontAtlas.Atlas.Path);
                }

                var textureResult = textureFileReader.LoadTextures([atlasFile]);
                if (textureResult.TryPickProblems(out var textureProblems, out var textureAssetsList))
                {
                    return textureProblems.Prepend("Failed to load font atlas texture");
                }

                var textureAsset = textureAssetsList.First();

                // Convert RGBA to RGB for font atlas (MSDF only needs RGB channels)
                var rgbaData = textureAsset.Data;
                var rgbPixels = new RGB[rgbaData.Pixels.Length];
                for (var i = 0; i < rgbaData.Pixels.Length; i++)
                {
                    var p = rgbaData.Pixels[i];
                    rgbPixels[i] = new RGB(p.R, p.G, p.B);
                }
                var rgbData = new TextureData<RGB>
                {
                    Pixels = rgbPixels,
                    Width = rgbaData.Width,
                    Height = rgbaData.Height,
                };

                // Override destination to fonts folder
                var fontName = System.IO.Path.GetFileNameWithoutExtension(textureAsset.Name);
                var fontAtlasDestination = $"fonts/{fontName}.texture";

                var writeResult = await assetWriter.WriteAssetAsync(rgbData, fontAtlasDestination, ct);
                if (writeResult.TryPickProblems(out var writeProblems))
                {
                    return writeProblems.Prepend("Failed to write font atlas texture");
                }

                logger.LogDebug("Processed font atlas texture: {Name}", fontName);
            }
        }

        logger.LogInformation(
            "Processed {MeshCount} mesh(es), {TextureCount} texture(s), {TerrainCount} terrain(s), and {FontCount} font(s) successfully!",
            meshAssets.Count,
            textureAssets.Count,
            terrainAssets.Count,
            fontFiles.Count);

        return new Response(meshAssets, textureAssets, terrainAssets, fontFiles);
    }
}