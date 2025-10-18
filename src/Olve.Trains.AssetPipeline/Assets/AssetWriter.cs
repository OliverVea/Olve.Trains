using MemoryPack;
using Microsoft.Extensions.Logging;
using Olve.Results;

namespace Olve.Trains.AssetPipeline.Assets;

public class AssetWriter(ILogger<AssetWriter> logger, PathProvider pathProvider)
{
    public async Task<Result> WriteAssetAsync<T>(T assetData, string destination, CancellationToken ct = default)
    {
        try
        {
            using var assetStream = new MemoryStream();

            await MemoryPackSerializer.SerializeAsync(assetStream, assetData, cancellationToken: ct);

            var assetOutputPath = pathProvider.OutputPath / destination;

            ReadOnlyMemory<byte> assetBytes = assetStream.GetBuffer();

            await File.WriteAllBytesAsync(assetOutputPath.Path, assetBytes, ct);

            logger.LogDebug("Wrote asset '{MeshOutputPath} with '{SizeInBytes}' bytes of data.", assetOutputPath,
                assetBytes.Length);

            return Result.Success();
        }
        catch (Exception e)
        {
            return new ResultProblem(e, "Failed to write asset '{0}'", destination);
        }
    }
}