using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;

namespace Olve.Engine3D.Camera;

public static class Camera
{
    public static Camera<TView, TProjection> Create<TView, TProjection>(TView view, TProjection projection)
        where TView : IView
        where TProjection : IProjection
    {
        return new Camera<TView, TProjection>(view, projection);
    }
}

public class Camera<TView, TProjection>(TView view, TProjection projection) : ICamera
    where TView : IView
    where TProjection : IProjection
{
    public TView View { get; set; } = view;
    public TProjection Projection { get; set; } = projection;

    public Matrix4X4<float> GetViewMatrix() => View.GetViewMatrix();
    public Matrix4X4<float> GetProjectionMatrix() => Projection.GetProjectionMatrix();
}