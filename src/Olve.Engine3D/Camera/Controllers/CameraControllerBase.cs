using Microsoft.Xna.Framework;

namespace Olve.Engine3D.Camera.Controllers;

public abstract class CameraControllerBase<TCamera>(TCamera camera) : ICameraController
    where TCamera : class, ICamera
{
    protected readonly TCamera Camera = camera;

    ICamera ICameraController.Camera => Camera;

    public abstract void Update(GameTime gameTime);
}