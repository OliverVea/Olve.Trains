namespace Olve.Trains.Terrain;

public interface ITerrainGenerator
{
    Terrain Generate(int width, int length, int? seed = null);
}