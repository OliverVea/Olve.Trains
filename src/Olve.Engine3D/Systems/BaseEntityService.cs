using System.Diagnostics.CodeAnalysis;
using Olve.Logging;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public abstract class BaseEntityService<TValue> where TValue : IHasId<Id<TValue>>
{
    protected readonly ILoggingManager LoggingManager;
    
    private readonly SortedList<Id<TValue>, TValue> _entities = [];
    private readonly string _valueTypeName = typeof(TValue).Name;
    private readonly string[] _loggingTags;
    private readonly Lock _entityLock = new();

    private Lazy<HashSet<Id<TValue>>> _entityIds = new(() => []);
    private Lazy<HashSet<TValue>> _entityValues = new(() => []);
    
    public IReadOnlyCollection<Id<TValue>> Ids => _entityIds.Value; 
    public IReadOnlyCollection<Id<TValue>> Entities => _entityIds.Value; 

    private HashSet<Id<TValue>> GetAllIds()
    {
        lock (_entityLock)
        {
            return _entities.Keys.ToHashSet();
        } 
    }

    private HashSet<TValue> GetAllValues()
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
    
    public Action<Id<TValue>>? OnAdded { get; set; }
    public Action<Id<TValue>>? OnRemoved { get; set; }

    public int Count => _entities.Count;
    
    protected Result<Id<TValue>> Add(TValue entity)
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
        _entityIds = new Lazy<HashSet<Id<TValue>>>(GetAllIds);
        _entityValues = new Lazy<HashSet<TValue>>(GetAllValues);
        OnAdded?.Invoke(entity.Id);
        
        LoggingManager.Log(LogLevel.Debug, $"Added entity of type '{_valueTypeName}' with id '{entity.Id}'", _loggingTags);
        
        return entity.Id;
    }
    
    public DeletionResult Remove(Id<TValue> id)
    {
        if (!_entities.ContainsKey(id))
        {
            LoggingManager.Log(LogLevel.Warning, $"Tried to remove non-existing entity of type '{_valueTypeName}' with id '{id}'", _loggingTags);
            return DeletionResult.NotFound();
        }
        
        OnRemoved?.Invoke(id);

        lock (_entityLock)
        {
            _entities.Remove(id);
        }
        
        _entityIds = new Lazy<HashSet<Id<TValue>>>(GetAllIds);
        _entityValues = new Lazy<HashSet<TValue>>(GetAllValues);
        
        LoggingManager.Log(LogLevel.Debug, $"Removed entity of type '{_valueTypeName}' with id '{id}'", _loggingTags);
        
        return DeletionResult.Success();
    }
    
    public bool TryGet(Id<TValue> id, [MaybeNullWhen(false)] out TValue entity)
    {
        return _entities.TryGetValue(id, out entity);
    }

    public Result<TValue> Get(Id<TValue> id)
    {
        if (TryGet(id, out var entity))
        {
            return entity;
        }
        
        return new ResultProblem("Did not find entity with id '{0}'", id);
    }
    
    public bool Exists(Id<TValue> id) => _entities.ContainsKey(id);
}