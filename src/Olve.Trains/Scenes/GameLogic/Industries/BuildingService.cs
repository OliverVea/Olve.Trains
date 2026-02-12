using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public class BuildingService() : BaseEntityService<Building>(NullLogger.Instance);