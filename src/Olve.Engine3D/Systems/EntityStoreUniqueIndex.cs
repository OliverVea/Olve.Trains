using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public sealed class EntityStoreUniqueIndex<T, TKey> : IDisposable
    where T : IHasId<Id<T>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, Id<T>> _index = new();
    private readonly EntityStore<T> _store;
    private readonly Func<T, TKey> _keySelector;

    internal EntityStoreUniqueIndex(EntityStore<T> store, Func<T, TKey> keySelector)
    {
        _store = store;
        _keySelector = keySelector;

        store.OnAdded.Subscribe(OnAdded);
        store.OnRemoved.Subscribe(OnRemoved);
    }

    private void OnAdded(Id<T> id)
    {
        if (!_store.TryGet(id, out var entity)) return;
        _index[_keySelector(entity)] = id;
    }

    private void OnRemoved(Id<T> id)
    {
        if (!_store.TryGet(id, out var entity)) return;
        _index.Remove(_keySelector(entity));
    }

    public bool TryGet(TKey key, out Id<T> id) => _index.TryGetValue(key, out id);
    public bool ContainsKey(TKey key) => _index.ContainsKey(key);

    public void Dispose()
    {
        _store.OnAdded.Unsubscribe(OnAdded);
        _store.OnRemoved.Unsubscribe(OnRemoved);
        _index.Clear();
    }
}
