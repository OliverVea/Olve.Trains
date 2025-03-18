namespace Olve.Engine3D.Camera.Controllers;

public abstract class CameraControllerBase<TCamera>(TCamera camera) : ICameraController
    where TCamera : class, ICamera
{
    public readonly TCamera Camera = camera;

    ICamera ICameraController.Camera => Camera;

    public abstract void Move(Vector3D<float> direction, TimeSpan deltaTime, float scale = 1f);
    public abstract void Rotate(Vector2D<float> rotation, TimeSpan deltaTime, float scale = 1f);
    public abstract void Zoom(float delta, TimeSpan deltaTime, float scale = 1f);
}