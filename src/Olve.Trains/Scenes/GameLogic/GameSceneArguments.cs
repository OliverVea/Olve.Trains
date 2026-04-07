using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Time;
using Olve.Trains.Scenes.GameLogic.Money;

namespace Olve.Trains.Scenes.GameLogic;

public sealed record GameSceneArguments(
    int StartingMoney = MoneyConstants.StartingBalance,
    HeightmapData? Heightmap = null,
    int TreeSeed = 42,
    double TreeSpawnProbability = 0.08,
    DayTime? DayStart = null,
    TimeSpan? DayDuration = null,
    float CameraOrthographicSize = 40f)
{
    public static HeightmapData DefaultHeightmap()
    {
        const int length = 50;
        const int width = 50;

        var heights = new int[length * width];
        Array.Fill(heights, 1);

        for (var z = 3; z <= 6; z++)
        {
            for (var x = 12; x <= 15; x++)
            {
                heights[z * width + x] = 2;
            }
        }

        return new HeightmapData
        {
            Heights = heights,
            Width = width,
            Length = length,
            Step = 0.125f,
        };
    }
}
