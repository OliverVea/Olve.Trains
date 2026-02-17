using Olve.Utilities.CollectionExtensions;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Stations;

public class StationNameGenerator
{
    public string CreateStationName()
    {
        return StationPool.Yamanote.StationNames.PickRandom();
    }
}