using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Resources;

public readonly record struct ResourceType(Id<ResourceType> Id, string Name) : IHasId<Id<ResourceType>>;
