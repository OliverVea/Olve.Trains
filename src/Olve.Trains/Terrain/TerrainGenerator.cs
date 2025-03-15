namespace Olve.Trains.Terrain;

public class TerrainGenerator(
    int baseHeight = 0,
    int minHeight = 0,
    int maxHeight = 10,
    float primaryRoughness = 10,
    float? secondaryRoughness = 10,
    float? variationBias = 10,
    float heightVariationFactor = 10)  // Added parameter for height variation factor
    : ITerrainGenerator
{
    public Terrain Generate(int width, int length, int? seed = null)
    {
        var terrain = new Terrain(width, length);

        for (var z = 0; z <= length; z++)
        {
            for (var x = 0; x <= width; x++)
            {
                GridCoordinate coordinate = new(x, z);

                terrain[coordinate] = x / 10;
            }
        }

        return terrain;
    }
}
