using ControllerCamera = Olve.Engine3D.Camera.Camera<Olve.Engine3D.Camera.Views.IsometricView, Olve.Engine3D.Camera.Projections.PerspectiveProjection>;


namespace Olve.Engine3D.Camera.Controllers;

public class IsometricPerspectiveCameraController(ControllerCamera camera)
    : CameraControllerBase<ControllerCamera>(camera)
{
    public override void Move(Vector3D<float> direction, TimeSpan deltaTime, float scale = 1)
    {
        throw new NotImplementedException();
    }

    public override void Rotate(Vector2D<float> rotation, TimeSpan deltaTime, float scale = 1)
    {
        throw new NotImplementedException();
    }

    public override void Zoom(float delta, TimeSpan deltaTime, float scale = 1)
    {
        throw new NotImplementedException();
    }
}