using Olve.Engine3D.Systems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Olve.Trains.Scenes.Game.Stations;

public class StationService() : BaseEntityService<Station>(NullLogger.Instance)
{
    public Result<Id<Station>> CreateStation(string name, Vector3D<float> center)
    {
        var id = Id.New<Station>();
        Station station = new(id, name, center);

        return Add(station);
    }
}