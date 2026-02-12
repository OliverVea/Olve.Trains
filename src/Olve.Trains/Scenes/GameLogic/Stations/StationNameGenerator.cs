using Olve.Utilities.CollectionExtensions;

namespace Olve.Trains.Scenes.GameLogic.Stations;

public class StationNameGenerator
{
    public string CreateStationName()
    {
        return StationPool.Yamanote.StationNames.PickRandom();
    }
}