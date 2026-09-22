using Olve.Utilities.Lookup;
using Olve.Utilities.Stores;

namespace Olve.Engine3D.Stores;

public static class EntityStoreExtensions
{
    public static OrderedEntityStoreValueCache<T, TId> BuildOrderedValueCache<T, TId>(
        this EntityStore<T, TId> entityStore, Comparer<T> comparer) where T : IHasId<TId> where TId : notnull
    {
        return new OrderedEntityStoreValueCache<T, TId>(entityStore, comparer);
    }
}
