using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Stations;

public class StationService
{
    private readonly EntityStore<Station> _stations = new();

    public Id<Station> CreateStation(string name, Vector3D<float> center)
    {
        Station station = new(Id.New<Station>(), name, center);
        _stations.Set(station);
        return station.Id;
    }
}