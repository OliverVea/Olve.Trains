using System.Collections;
using Olve.Utilities.Lookup;
using Olve.Utilities.Stores;

namespace Olve.Engine3D.Stores;

public sealed class OrderedEntityStoreValueCache<T, TId> : IDisposable, IReadOnlyList<T>  where T : IHasId<TId> where TId : notnull
{
    private readonly EntityStore<T, TId> _entityStore;
    private readonly IComparer<T> _comparer;
    
    private T[]? _orderedValues;
    
    private void Clear(TId _) => _orderedValues = null;

    internal OrderedEntityStoreValueCache(EntityStore<T, TId> entityStore, IComparer<T> comparer)
    {
        _entityStore = entityStore;
        _comparer = comparer;
        
        _entityStore.OnAdded.Subscribe(Clear);
        _entityStore.OnUpdated.Subscribe(Clear);
        _entityStore.OnDeleted.Subscribe(Clear);
    }

    public void Dispose()
    {
        _entityStore.OnAdded.Unsubscribe(Clear);
        _entityStore.OnUpdated.Unsubscribe(Clear);
        _entityStore.OnDeleted.Unsubscribe(Clear);
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>) GetOrderedValues()).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetOrderedValues().GetEnumerator();

    public int Count => _entityStore.Count;

    public T this[int index] => GetOrderedValues()[index];

    private T[] GetOrderedValues()
    {
        if (_orderedValues != null) return _orderedValues;

        _orderedValues = _entityStore.List()
            .Order(_comparer)
            .ToArray();

        return _orderedValues;
    }
}
