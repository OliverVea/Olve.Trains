using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public class IndustryService() : BaseEntityService<Industry>(NullLogger.Instance);