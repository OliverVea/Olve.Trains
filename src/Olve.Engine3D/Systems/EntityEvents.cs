using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public static class EntityEvents
{
    /// <summary>Each entity as the event its store fired when it was added, e.g. to prefill a scene event subscription.</summary>
    public static IEnumerable<EntityAdded<T, Id<T>>> AsAdded<T>(this IEnumerable<T> entities)
        where T : IHasId<Id<T>>
        => entities.Select(entity => new EntityAdded<T, Id<T>>(entity.Id, entity));
}
