using Microsoft.Xna.Framework;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;

namespace Olve.Engine3D.Camera.Cameras;

public class Camera<TView, TProjection>(TView view, TProjection projection) : ICamera
    where TView : IView
    where TProjection : IProjection
{
    public TView View { get; set; } = view;
    public TProjection Projection { get; set; } = projection;

    public Matrix GetViewMatrix() => View.GetViewMatrix();
    public Matrix GetProjectionMatrix() => Projection.GetProjectionMatrix();
}