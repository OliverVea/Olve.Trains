using Olve.Engine3D.IO.Images;
using Olve.OpenRaster;
using Olve.Utilities.Types.Results;

namespace Olve.Trains.Terrain;

public static class TerrainLoader
{
    public static Result<TriMesh> LoadTerrain(MapFilePath mapFilePath)
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
            .Where(x => x.Name == "heightmap")
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

        MeshLayerReader meshLayerReader = new();
        
        GetLayerImage<TriMesh> getLayerMesh = new();
        GetLayerImage<TriMesh>.Request getLayerMeshRequest = new(mapFilePath.FilePath, terrainLayerFile.Source, meshLayerReader);
        
        var getLayerImageResult = getLayerMesh.Execute(getLayerMeshRequest);
        if (getLayerImageResult.TryPickProblems(out var getLayerImageProblems, out var getLayerImageResponse))
        {
            getLayerImageProblems.Prepend(new ResultProblem("Failed to load terrain layer"));
            return getLayerImageProblems;
        }

        return getLayerImageResponse.Image;
    }
}