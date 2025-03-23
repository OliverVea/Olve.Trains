namespace Olve.Engine3D.Rendering;

public interface IRenderingEntityManager<T>
{
    Result<RenderingEntityId<T>> Register(T entity);
    Result Unregister(RenderingEntityId<T> entityId);
}