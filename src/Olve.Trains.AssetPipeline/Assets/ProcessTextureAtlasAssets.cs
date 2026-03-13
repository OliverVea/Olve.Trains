using System.Text.Json;
using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets.Entities;
using Scriban.Runtime;

namespace Olve.Trains.AssetPipeline.Assets;

public class ProcessTextureAtlasAssets(
    ILogger<ProcessTextureAtlasAssets> logger,
    NamespaceProvider namespaceProvider,
    PathProvider pathProvider,
    TextureFileReader textureFileReader,
    AssetWriter assetWriter,
    TemplateWriter templateWriter)
{
    private static readonly string TemplateFileName = "TextureAtlasClass.scriban";

    public record Request(IReadOnlyList<FileInfo> AssetFiles);
    public record Response(IReadOnlyList<IPath> GeneratedFiles);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Processing texture atlas assets");

        var jsonFiles = request.AssetFiles
            .Where(f => f.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (jsonFiles.Count == 0)
        {
            logger.LogDebug("No texture atlas JSON files found");
            return new Response([]);
        }

        pathProvider.TextureAtlasesOutputFolder.EnsurePathExists();

        var templatePath = pathProvider.TemplatesSourceFolder / TemplateFileName;
        List<IPath> generatedFiles = [];

        foreach (var jsonFile in jsonFiles)
        {
            var processResult = await ProcessAtlasAsync(jsonFile, templatePath, request.AssetFiles, ct);
            if (processResult.TryPickProblems(out var problems, out var generatedFile))
            {
                return problems.Prepend("Failed to process texture atlas '{0}'", jsonFile.Name);
            }

            generatedFiles.Add(generatedFile);
        }

        logger.LogInformation("Processed {Count} texture atlas(es) successfully", generatedFiles.Count);
        return new Response(generatedFiles);
    }

    private async Task<Result<IPath>> ProcessAtlasAsync(
        FileInfo jsonFile,
        IPath templatePath,
        IReadOnlyList<FileInfo> assetFiles,
        CancellationToken ct)
    {
        var jsonText = await File.ReadAllTextAsync(jsonFile.FullName, ct);
        var atlas = JsonSerializer.Deserialize<AtlasJson>(jsonText, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
        if (atlas?.Sprites is null)
        {
            return new ResultProblem("Failed to parse atlas JSON '{0}'", jsonFile.Name);
        }

        // Find the corresponding PNG
        var pngName = atlas.Image ?? System.IO.Path.GetFileNameWithoutExtension(jsonFile.Name) + ".png";
        var pngFile = assetFiles.FirstOrDefault(f =>
            f.Name.Equals(pngName, StringComparison.OrdinalIgnoreCase));
        if (pngFile is null)
        {
            return new ResultProblem("Atlas image '{0}' not found for '{1}'", pngName, jsonFile.Name);
        }

        // Load and write the texture
        var textureResult = textureFileReader.LoadTextures([pngFile]);
        if (textureResult.TryPickProblems(out var problems, out var textures))
        {
            return problems.Prepend("Failed to load atlas texture '{0}'", pngName);
        }

        var textureAsset = textures.First();
        var atlasName = System.IO.Path.GetFileNameWithoutExtension(jsonFile.Name);
        var className = ToPascalCase(atlasName);
        var textureDestination = $"atlases/{atlasName}.texture";

        var writeResult = await assetWriter.WriteAssetAsync(textureAsset.Data, textureDestination, ct);
        if (writeResult.TryPickProblems(out var writeProblems))
        {
            return writeProblems.Prepend("Failed to write atlas texture");
        }

        // Build sprite script objects
        var spriteObjects = atlas.Sprites
            .Select(kvp =>
            {
                var so = new ScriptObject
                {
                    { "Name", ToPascalCase(kvp.Key) },
                    { "X", kvp.Value.X },
                    { "Y", kvp.Value.Y },
                    { "W", kvp.Value.W },
                    { "H", kvp.Value.H },
                };
                return so;
            })
            .ToArray();

        var scriptObject = new ScriptObject
        {
            { "Namespace", namespaceProvider.TextureAtlasNamespace },
            {
                "Atlas", new ScriptObject
                {
                    { "ClassName", className },
                    {
                        "Texture", new ScriptObject
                        {
                            { "Name", atlasName },
                            { "Path", textureDestination },
                        }
                    },
                    {
                        "Size", new ScriptObject
                        {
                            { "X", atlas.Width },
                            { "Y", atlas.Height },
                        }
                    },
                    { "Sprites", spriteObjects },
                }
            },
        };

        var destinationPath = pathProvider.TextureAtlasesOutputFolder / (className + ".cs");

        var templateResult = await templateWriter.WriteTemplateAsync(templatePath, scriptObject, destinationPath, ct);
        if (templateResult.TryPickProblems(out var templateProblems))
        {
            return templateProblems.Prepend("Failed to write texture atlas class for '{0}'", atlasName);
        }

        logger.LogDebug("Generated texture atlas class: {Destination}", destinationPath);
        return Result.Success<IPath>(destinationPath);
    }

    private static string ToPascalCase(string input)
    {
        var parts = input.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p =>
            char.ToUpperInvariant(p[0]) + p[1..]));
    }

    private record AtlasJson(
        string? Image,
        int Width,
        int Height,
        Dictionary<string, SpriteJson>? Sprites);

    private record SpriteJson(int X, int Y, int W, int H);
}
