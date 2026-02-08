using Olve.Engine3D;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.Game.Industries;

public class IndustryService(ILoggingManager loggingManager) : BaseEntityService<Industry>(loggingManager);

public readonly record struct Industry(Id<Industry> Id, Id<Building> BuildingId) : IHasId<Id<Industry>>;


public class BuildingService(ILoggingManager loggingManager) : BaseEntityService<Building>(loggingManager);

public class BuildingOccupancyService
{

}

public readonly record struct TileFootprint(int Width, int Height, int Depth);

public readonly record struct Building(Id<Building> Id, TilePosition Origin, TileFootprint Footprint) : IHasId<Id<Building>>;