using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public sealed class IdDictionary<T> : Dictionary<Id<T>, T>
    where T : IHasId<Id<T>>;