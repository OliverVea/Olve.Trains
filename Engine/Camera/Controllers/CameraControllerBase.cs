using Microsoft.Xna.Framework;

namespace Engine.Camera.Controllers;

public abstract class CameraControllerBase<TCamera>(TCamera orthographicCamera) : ICameraController
    where TCamera : class, ICamera
{
    protected readonly TCamera OrthographicCamera = orthographicCamera;

    ICamera ICameraController.Camera => OrthographicCamera;

    public abstract void Update(GameTime gameTime);
}