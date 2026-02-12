using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Stations;

public class StationService() : BaseEntityService<Station>(NullLogger.Instance)
{
    public Result<Id<Station>> CreateStation(string name, Vector3D<float> center)
    {
        var id = Id.New<Station>();
        Station station = new(id, name, center);

        return Add(station);
    }
}