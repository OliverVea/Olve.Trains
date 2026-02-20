using Microsoft.Extensions.Logging;
using Olve.Engine3D.Assets.Entities;
using Olve.OpenRaster;

namespace Olve.Trains.AssetPipeline.Assets;

public class TerrainFileReader(ILogger<TerrainFileReader> logger, ReadOpenRasterFile readOpenRasterFile, ReadLayerAs<HeightmapData> readLayerAsHeightmap, ILayerParser<HeightmapData> heightmapLayerParser)
{
    private const string HeightmapLayerName = "heightmap";

    public Result<IReadOnlyList<Asset<TerrainData>>> LoadAssets(IReadOnlyList<FileInfo> files)
    {
        var terrainFiles = files
            .Where(f => f.Name.Contains(".terrain.") && f.Extension is ".ora")
            .Select(f => f.FullName)
            .ToArray();

        if (terrainFiles.Length == 0)
        {
            logger.LogWarning("No terrain files found");
            return Array.Empty<Asset<TerrainData>>();
        }

        logger.LogDebug("Processing {ModelCount} terrains", terrainFiles.Length);

        var terrainResults = terrainFiles.Select(LoadTerrain).ToList();
        if (terrainResults.TryPickProblems(out var problems, out var terrains))
        {
            return problems.Prepend("Failed to load terrains");
        }

        logger.LogDebug("Processed {ModelCount} terrains", terrainFiles.Length);

        return Result.Success((IReadOnlyList<Asset<TerrainData>>)terrains);
    }

    private Result<Asset<TerrainData>> LoadTerrain(string assetSource)
    {
        if (!File.Exists(assetSource))
        {
            return new ResultProblem("Terrain file '{0}' does not exist", assetSource);
        }

        ReadOpenRasterFile.Request readFileRequest = new(assetSource);
        if (readOpenRasterFile.Execute(readFileRequest).TryPickProblems(out var problems, out var openRasterFile))
        {
            return problems;
        }

        if (GetLayer(assetSource, openRasterFile, HeightmapLayerName)
            .TryPickProblems(out problems, out var heightmapLayer))
        {
            return problems;
        }

        ReadLayerAs<HeightmapData>.Request readLayerRequest = new(
            assetSource,
            heightmapLayer.Source,
            heightmapLayerParser);

        if (readLayerAsHeightmap.Execute(readLayerRequest).TryPickProblems(out problems, out var heightmapData))
        {
            return problems;
        }

        var terrainData = new TerrainData
        {
            Heightmap = heightmapData
        };

        var assetName = System.IO.Path.GetFileNameWithoutExtension(assetSource).Split('.')[0];
        var assetDestination = "terrains/" + assetName + ".terrain";

        return new Asset<TerrainData>
        {
            Name = assetName,
            Source = assetSource,
            Destination = assetDestination,
            Data = terrainData
        };
    }

    private static Result<Layer> GetLayer(string assetSource, OpenRasterFile openRasterFile, string layerName)
    {
        var matchingLayers = openRasterFile.Layers.Where(l => l.Name == layerName).ToList();
        if (matchingLayers.Count != 1)
        {
            return new ResultProblem("Expected 1 terrain layer in file '{0}', but found {1}", assetSource, matchingLayers.Count);
        }

        return matchingLayers[0];
    }

}