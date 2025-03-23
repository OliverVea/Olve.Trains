namespace Olve.Engine3D.Rendering.OpenGL;

public interface IOpenGLEntityManager<in T, TRegistration>
{
    Result<TRegistration> Register(T entityData);
    Result Unregister(TRegistration registration);
}