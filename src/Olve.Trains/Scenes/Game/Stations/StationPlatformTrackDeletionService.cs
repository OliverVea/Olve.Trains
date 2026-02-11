using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Stations;

public class StationPlatformTrackDeletionService(TrackService trackService, StationPlatformService stationPlatformService)
    : BaseEntityListeningService<Track>(trackService)
{
    protected override (bool SubscribeAdd, bool SubscribeDelete) GetSubscriptions() => (false, true);

    protected override Result OnRemoved(Id<Track> trackId)
    { 
        return stationPlatformService.RemoveForTrack(trackId).MapToResult();
    }
}