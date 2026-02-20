using Olve.Utilities.CollectionExtensions;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public sealed class EntityStoreIndex<T, TKey> : IDisposable
    where T : IHasId<Id<T>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, HashSet<Id<T>>> _index = new();
    private readonly EntityStore<T> _store;
    private readonly Func<T, TKey> _keySelector;

    internal EntityStoreIndex(EntityStore<T> store, Func<T, TKey> keySelector)
    {
        _store = store;
        _keySelector = keySelector;

        store.OnAdded.Subscribe(OnAdded);
        store.OnRemoved.Subscribe(OnRemoved);
    }

    private void OnAdded(Id<T> id)
    {
        if (!_store.TryGet(id, out var entity)) return;

        var key = _keySelector(entity);
        _index.GetOrAdd(key, () => []).Add(id);
    }

    private void OnRemoved(Id<T> id)
    {
        if (!_store.TryGet(id, out var entity)) return;

        var key = _keySelector(entity);
        if (!_index.TryGetValue(key, out var ids)) return;

        ids.Remove(id);
        if (ids.Count == 0) _index.Remove(key);
    }

    public IReadOnlyCollection<Id<T>> GetForKey(TKey key)
    {
        return _index.TryGetValue(key, out var ids) ? ids : [];
    }

    public bool ContainsKey(TKey key) => _index.ContainsKey(key);

    public void Dispose()
    {
        _store.OnAdded.Unsubscribe(OnAdded);
        _store.OnRemoved.Unsubscribe(OnRemoved);
        _index.Clear();
    }
}
