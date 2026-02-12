using Olve.Engine3D.Systems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Olve.Trains.Scenes.Game.Industries;

public class BuildingService() : BaseEntityService<Building>(NullLogger.Instance);