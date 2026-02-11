using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public abstract class BaseEntityService<TEntity>(ILogger logger) : IEntityService<TEntity>
    where TEntity : IHasId<Id<TEntity>>
{

    private readonly SortedList<Id<TEntity>, TEntity> _entities = [];
    private readonly string _valueTypeName = typeof(TEntity).Name;
    private readonly Lock _entityLock = new();

    private Lazy<HashSet<Id<TEntity>>> _entityIds = new(static () => []);
    private Lazy<HashSet<TEntity>> _entityValues = new(static () => []);

    public IReadOnlyCollection<Id<TEntity>> Ids => _entityIds.Value;
    public IReadOnlyCollection<TEntity> Entities => _entityValues.Value;

    private HashSet<Id<TEntity>> GetAllIds()
    {
        lock (_entityLock)
        {
            return _entities.Keys.ToHashSet();
        }
    }

    private HashSet<TEntity> GetAllValues()
    {
        lock (_entityLock)
        {
            return _entities.Values.ToHashSet();
        }
    }

    private string ServiceTypeName => GetType().Name;

    public Event<Id<TEntity>> OnAdded { get; } = new();
    public Event<Id<TEntity>> OnRemoved { get; } = new();

    public int Count => _entities.Count;

    protected Result<Id<TEntity>> Add(TEntity entity)
    {
        if (_entities.ContainsKey(entity.Id))
        {
            logger.LogWarning("Tried to add already existing entity of type '{ValueTypeName}' with id '{EntityId}'", _valueTypeName, entity.Id);
            return new ResultProblem("Entity with id '{0}' was already found", entity.Id);
        }

        lock (_entityLock)
        {
            _entities.Add(entity.Id, entity);
        }
        _entityIds = new Lazy<HashSet<Id<TEntity>>>(GetAllIds);
        _entityValues = new Lazy<HashSet<TEntity>>(GetAllValues);
        OnAdded.Invoke(entity.Id);

        logger.LogDebug("Added entity of type '{ValueTypeName}' with id '{EntityId}'", _valueTypeName, entity.Id);

        return entity.Id;
    }

    public virtual DeletionResult Remove(Id<TEntity> id)
    {
        if (!_entities.ContainsKey(id))
        {
            logger.LogWarning("Tried to remove non-existing entity of type '{ValueTypeName}' with id '{EntityId}'", _valueTypeName, id);
            return DeletionResult.NotFound();
        }

        OnRemoved.Invoke(id);

        lock (_entityLock)
        {
            _entities.Remove(id);
        }

        _entityIds = new Lazy<HashSet<Id<TEntity>>>(GetAllIds);
        _entityValues = new Lazy<HashSet<TEntity>>(GetAllValues);

        logger.LogDebug("Removed entity of type '{ValueTypeName}' with id '{EntityId}'", _valueTypeName, id);

        return DeletionResult.Success();
    }

    public bool TryGet(Id<TEntity> id, [MaybeNullWhen(false)] out TEntity entity)
    {
        return _entities.TryGetValue(id, out entity);
    }

    public Result<TEntity> Get(Id<TEntity> id)
    {
        if (TryGet(id, out var entity))
        {
            return entity;
        }

        return new ResultProblem("Did not find entity with id '{0}'", id);
    }

    public bool Exists(Id<TEntity> id) => _entities.ContainsKey(id);
}
