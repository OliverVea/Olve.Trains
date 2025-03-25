using BigGustave;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Rendering.Entities;
using Olve.Results;
using Silk.NET.Maths;

namespace Olve.Trains.AssetPipeline.Assets;

public class TextureFileReader(ILogger<TextureFileReader> logger)
{
    public Result<IReadOnlyList<Asset<TextureData>>> LoadTextures(IReadOnlyList<FileInfo> files)
    {
        var pngFiles = files
            .Where(f => f.Extension is ".png")
            .Select(f => f.FullName)
            .ToArray();

        if (pngFiles.Length == 0)
        {
            logger.LogWarning("No texture files found");
            return Array.Empty<Asset<TextureData>>();
        }

        logger.LogInformation("Processing {ModelCount} textures", pngFiles.Length);

        var pngResults = pngFiles.Select(LoadTexture).ToList();
        if (pngResults.TryPickProblems(out var problems, out var textures))
        {
            return problems.Prepend("Failed to load textures");
        }

        return Result.Success((IReadOnlyList<Asset<TextureData>>)textures);
    }

    private Result<Asset<TextureData>> LoadTexture(string assetSource)
    {
        if (!File.Exists(assetSource))
        {
            return new ResultProblem("Texture file '{0}' does not exist", assetSource);
        }

        var png = Png.Open(assetSource);

        int width = png.Width, height = png.Height;

        var pixelData = new Vector4D<byte>[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = png.GetPixel(x, height - y - 1);
                pixelData[y * width + x] = new Vector4D<byte>(pixel.R, pixel.G, pixel.B, pixel.A);
            }
        }

        TextureData textureData = new()
        {
            Width = width,
            Height = height,
            Pixels = pixelData,
        };

        var validation = textureData.Validate();
        if (validation.TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to validate texture data");
        }

        var assetName = Path.GetFileNameWithoutExtension(assetSource).Split('.')[0];
        var assetDestination = $"textures/{assetName}.texture";

        return new Asset<TextureData>
        {
            Name = assetName,
            Source = assetSource,
            Destination = assetDestination,
            Data = textureData
        };
    }
}