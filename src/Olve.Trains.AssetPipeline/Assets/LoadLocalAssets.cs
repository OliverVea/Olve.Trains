using Microsoft.Extensions.Logging;
using Olve.Paths.Glob;

namespace Olve.Trains.AssetPipeline.Assets;

/// <summary>
///    Loads source art from the committed asset source directory (tracked via git LFS)
/// </summary>
public class LoadLocalAssets(ILogger<LoadLocalAssets> logger, PathProvider pathProvider)
{
    public record Request;
    public record Response(IReadOnlyList<FileInfo> Files);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Loading source art from the asset source directory");

        var sourcePath = pathProvider.AssetsSourceFolder;

        if (!sourcePath.Exists())
        {
            return new ResultProblem("Asset source directory does not exist: {0}", sourcePath);
        }

        // Find all files in the source directory
        var hasFiles = sourcePath.TryGlob("**/*", out var paths);
        if (!hasFiles)
        {
            logger.LogWarning("No files found in asset source directory: {SourcePath}", sourcePath);
            return new Response([]);
        }

        var files = (paths?
            .Where(p => p.ElementType == ElementType.File)
            .Select(p => new FileInfo(p.Absolute.Path))
            .Where(f => f.Exists) ?? [])
            .ToList();

        logger.LogInformation("Loaded {FileCount} source file(s) from: {SourcePath}", files.Count, sourcePath);

        foreach (var file in files)
        {
            logger.LogDebug("Found source asset: {FileName}", file.Name);
        }

        return new Response(files);
    }
}
