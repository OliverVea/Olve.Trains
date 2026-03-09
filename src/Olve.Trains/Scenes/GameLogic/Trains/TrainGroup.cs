using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public readonly record struct TrainGroup(Id<TrainGroup> Id, string Name) : IHasId<Id<TrainGroup>>;