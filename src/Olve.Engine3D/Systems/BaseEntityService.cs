using System.Diagnostics.CodeAnalysis;
using Olve.Logging;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public abstract class BaseEntityService<TEntity> : IEntityService<TEntity> where TEntity : IHasId<Id<TEntity>>
{
    protected readonly ILoggingManager LoggingManager;
    
    private readonly SortedList<Id<TEntity>, TEntity> _entities = [];
    private readonly string _valueTypeName = typeof(TEntity).Name;
    private readonly string[] _loggingTags;
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

    protected BaseEntityService(ILoggingManager loggingManager)
    {
        LoggingManager = loggingManager;
        _loggingTags = [_valueTypeName, ServiceTypeName];
    }

    private string ServiceTypeName => GetType().Name;

    public Event<Id<TEntity>> OnAdded { get; } = new();
    public Event<Id<TEntity>> OnRemoved { get; } = new();

    public int Count => _entities.Count;
    
    protected Result<Id<TEntity>> Add(TEntity entity)
    {
        if (_entities.ContainsKey(entity.Id))
        {
            LoggingManager.Log(LogLevel.Warning, $"Tried to add already existing entity of type '{_valueTypeName}' with id '{entity.Id}'", _loggingTags);
            return new ResultProblem("Entity with id '{0}' was already found", entity.Id);
        }

        lock (_entityLock)
        {
            _entities.Add(entity.Id, entity);
        }
        _entityIds = new Lazy<HashSet<Id<TEntity>>>(GetAllIds);
        _entityValues = new Lazy<HashSet<TEntity>>(GetAllValues);
        OnAdded.Invoke(entity.Id);
        
        LoggingManager.Log(LogLevel.Debug, $"Added entity of type '{_valueTypeName}' with id '{entity.Id}'", _loggingTags);
        
        return entity.Id;
    }
    
    public virtual DeletionResult Remove(Id<TEntity> id)
    {
        if (!_entities.ContainsKey(id))
        {
            LoggingManager.Log(LogLevel.Warning, $"Tried to remove non-existing entity of type '{_valueTypeName}' with id '{id}'", _loggingTags);
            return DeletionResult.NotFound();
        }
        
        OnRemoved.Invoke(id);

        lock (_entityLock)
        {
            _entities.Remove(id);
        }
        
        _entityIds = new Lazy<HashSet<Id<TEntity>>>(GetAllIds);
        _entityValues = new Lazy<HashSet<TEntity>>(GetAllValues);
        
        LoggingManager.Log(LogLevel.Debug, $"Removed entity of type '{_valueTypeName}' with id '{id}'", _loggingTags);
        
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