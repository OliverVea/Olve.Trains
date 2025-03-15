using BigGustave;
using Olve.Engine3D.IO.Images;
using Olve.OpenRaster;
using Olve.Utilities.Types.Results;

namespace Olve.Trains.Terrain;

public interface ITerrainGenerator
{
    Terrain Generate(int width, int length, int? seed = null);
}

public readonly record struct MapFilePath(string FilePath);

public static class TerrainLoader
{
    public static Result<Terrain> LoadTerrain(MapFilePath mapFilePath)
    {
        ReadOpenRasterFile operation = new();
        ReadOpenRasterFile.Request request = new(mapFilePath.FilePath);
        
        var result = operation.Execute(request);
        if (result.TryPickProblems(out var problems, out var openRasterFileResponse))
        {
            problems.Prepend(new ResultProblem("Failed to load terrain"));
            return problems;
        }

        var terrainFile = openRasterFileResponse
            .StackFile
            .Layers
            .Where(x => x.Name == "terrain")
            .ToArray();

        if (terrainFile.Length == 0)
        {
            return new ResultProblem("map file '{0}' does not contain a terrain layer", mapFilePath.FilePath);
        }
        
        if (terrainFile.Length > 1)
        {
            return new ResultProblem("map file '{0}' contains more than one terrain layer", mapFilePath.FilePath);
        }
        
        var terrainLayerFile = terrainFile[0];

        PngFileReader pngFileReader = new();
        
        GetLayerImage<Png> getLayerImageOperation = new();
        GetLayerImage<Png>.Request getLayerImageRequest = new(mapFilePath.FilePath, terrainLayerFile.Source, pngFileReader);
        
        var getLayerImageResult = getLayerImageOperation.Execute(getLayerImageRequest);
        if (getLayerImageResult.TryPickProblems(out var getLayerImageProblems, out var getLayerImageResponse))
        {
            getLayerImageProblems.Prepend(new ResultProblem("Failed to load terrain layer"));
            return getLayerImageProblems;
        }
        
        var png = getLayerImageResponse.Image;

        var terrain = new Terrain(png.Width, png.Height);
        
        for (var i = 0; i < png.Width; i++)
        for (var j = 0; j < png.Height; j++)
        {
            var pixel = png.GetPixel(i, j);
            var height = (pixel.R - 128) / 8;

            GridCoordinate gridCoordinate = new(i, j);

            terrain[gridCoordinate] = height;
        }

        return terrain;
    }
}