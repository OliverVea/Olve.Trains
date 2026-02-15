using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public class EntityStoreFactory
{
    public EntityStore<T> Create<T>()
        where T : IHasId<Id<T>>
    {
        return new EntityStore<T>();
    }
}