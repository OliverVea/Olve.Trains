using BigGustave;
using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets.Entities;

namespace Olve.Trains.AssetPipeline.Assets;

public class TextureFileReader(ILogger<TextureFileReader> logger)
{
    public Result<IReadOnlyList<Asset<TextureData<RGBA>>>> LoadTextures(IReadOnlyList<FileInfo> files)
    {
        var pngFiles = files
            .Where(f => f.Extension is ".png")
            .Select(f => f.FullName)
            .ToArray();

        if (pngFiles.Length == 0)
        {
            logger.LogWarning("No texture files found");
            return Array.Empty<Asset<TextureData<RGBA>>>();
        }

        logger.LogDebug("Processing {ModelCount} textures", pngFiles.Length);

        var pngResults = pngFiles.Select(LoadTexture).ToList();
        if (pngResults.TryPickProblems(out var problems, out var textures))
        {
            return problems.Prepend("Failed to load textures");
        }

        logger.LogDebug("Processed {ModelCount} textures", pngFiles.Length);

        return Result.Success((IReadOnlyList<Asset<TextureData<RGBA>>>)textures);
    }

    private Result<Asset<TextureData<RGBA>>> LoadTexture(string assetSource)
    {
        if (!File.Exists(assetSource))
        {
            return new ResultProblem("Texture file '{0}' does not exist", assetSource);
        }

        var png = Png.Open(assetSource);

        int width = png.Width, height = png.Height;

        var pixelData = new RGBA[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = png.GetPixel(x, height - y - 1);
                pixelData[y * width + x] = new RGBA(
                    pixel.R / 255f,
                    pixel.G / 255f,
                    pixel.B / 255f,
                    pixel.A / 255f);
            }
        }

        TextureData<RGBA> textureData = new()
        {
            Width = width,
            Height = height,
            Pixels = pixelData,
        };

        var assetName = System.IO.Path.GetFileNameWithoutExtension(assetSource).Split('.')[0];
        var assetDestination = $"textures/{assetName}.texture";

        return new Asset<TextureData<RGBA>>
        {
            Name = assetName,
            Source = assetSource,
            Destination = assetDestination,
            Data = textureData
        };
    }
}