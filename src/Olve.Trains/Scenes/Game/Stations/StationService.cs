using Olve.Engine3D.Systems;
using Olve.Logging;

namespace Olve.Trains.Scenes.Game.Stations;

public class StationService(ILoggingManager loggingManager) : BaseEntityService<Station>(loggingManager)
{
    public Result<Id<Station>> CreateStation(string name, Vector3D<float> center)
    {
        var id = Id.New<Station>();
        Station station = new(id, name, center);

        return Add(station);
    }
}