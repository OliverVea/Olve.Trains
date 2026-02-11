using Olve.Engine3D.Systems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Olve.Trains.Scenes.Game.Industries;

public class IndustryService() : BaseEntityService<Industry>(NullLogger.Instance);