using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Paths.Glob;

namespace Olve.Trains.AssetPipeline.Assets;

/// <summary>
///    Loads assets from the local build directory instead of downloading from S3
/// </summary>
public class LoadLocalAssets(ILogger<LoadLocalAssets> logger, PathProvider pathProvider) : IAsyncOperation<LoadLocalAssets.Request, LoadLocalAssets.Response>
{
    public record Request;
    public record Response(IReadOnlyList<FileInfo> Files);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Loading assets from local build directory");

        var buildPath = pathProvider.BuildS3CachePath;

        if (!buildPath.Exists())
        {
            logger.LogWarning("Build directory does not exist: {BuildPath}. Creating empty directory.", buildPath);
            buildPath.EnsurePathExists();
            return new Response([]);
        }

        // Find all files in the build directory
        var hasFiles = buildPath.TryGlob("**/*", out var paths);
        if (!hasFiles)
        {
            logger.LogWarning("No files found in build directory: {BuildPath}", buildPath);
            return new Response([]);
        }

        var files = (paths?
            .Where(p => p.ElementType == ElementType.File)
            .Select(p => new FileInfo(p.Absolute.Path))
            .Where(f => f.Exists) ?? [])
            .ToList();

        logger.LogInformation("Loaded {FileCount} file(s) from build directory: {BuildPath}", files.Count, buildPath);

        foreach (var file in files)
        {
            logger.LogDebug("Found local asset: {FileName}", file.Name);
        }

        return new Response(files);
    }
}
