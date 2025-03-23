namespace Olve.Engine3D.Rendering;

public interface IRenderingInstanceManager<T> : IRenderingEntityManager<T>
{
    Result<RenderingInstanceId<T>> RegisterInstance(RenderingEntityId<T> entityId, Matrix4X4<float> worldMatrix);
    Result DeregisterInstance(RenderingInstanceId<T> instanceId);

    Result SetInstanceWorld(RenderingInstanceId<T> instanceId, Matrix4X4<float> worldMatrix);
    Result<Matrix4X4<float>> GetInstanceWorld(RenderingInstanceId<T> instanceId);
}