using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public sealed class IdDictionary<T> : Dictionary<Id<T>, T>
    where T : IHasId<Id<T>>;