using System.Diagnostics.CodeAnalysis;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public sealed class EntityStore<T> where T :  IHasId<Id<T>>
{
    private readonly IdDictionary<T> _entities = new();

    public Event<Id<T>> OnAdded { get; } = new();
    public Event<Id<T>> OnRemoved  { get; } = new();

    public bool TryAdd(T entity)
    {
        if (!_entities.TryAdd(entity.Id, entity))
        {
            return false;
        }

        OnAdded.Invoke(entity.Id);
        return true;
    }

    public void Set(T entity)
    {
        if (Exists(entity))
        {
            return;
        }

        _entities[entity.Id] = entity;
        OnAdded.Invoke(entity.Id);
    }

    public DeletionResult Remove(Id<T> id)
    {
        if (!_entities.ContainsKey(id))
        {
            return DeletionResult.NotFound();
        }

        OnRemoved.Invoke(id);
        _entities.Remove(id);

        return DeletionResult.Success();
    }

    public bool TryGet(Id<T> id, [MaybeNullWhen(false)] out T entity) => _entities.TryGetValue(id, out entity);
    public bool Exists(Id<T> id) => _entities.ContainsKey(id);
    public bool Exists(T entity) => _entities.TryGetValue(entity.Id, out var existing) && existing.Equals(entity);
}