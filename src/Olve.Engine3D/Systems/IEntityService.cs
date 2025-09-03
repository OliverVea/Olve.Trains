using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public interface IEntityService<TEntity> where TEntity : IHasId<Id<TEntity>>
{
    Event<Id<TEntity>> OnAdded { get; }
    Event<Id<TEntity>> OnRemoved { get; }
}