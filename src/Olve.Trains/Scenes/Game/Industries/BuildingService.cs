using Olve.Engine3D.Systems;
using Olve.Logging;

namespace Olve.Trains.Scenes.Game.Industries;

public class BuildingService(ILoggingManager loggingManager) : BaseEntityService<Building>(loggingManager);