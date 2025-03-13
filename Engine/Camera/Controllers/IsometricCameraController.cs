using Engine.Camera.Cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Camera.Controllers;

public class IsometricCameraController(IsometricOrthographicCamera orthographicCamera) : CameraControllerBase<IsometricOrthographicCamera>(orthographicCamera)
{
    private readonly IsometricOrthographicCamera _orthographicCamera = orthographicCamera;
    public float LinearSpeed { get; set; } = 2.0f;
    public float LinearSpeedFactor { get; set; } = 3.0f;

    public float ZoomSpeed { get; set; } = 3.0f;
    public float ZoomSpeedFactor { get; set; } = 3.0f;

    public List<KeyDirection> MovementKeys { get; init; } =
    [
        new(Keys.W, Vector3.Backward),
        new(Keys.A, Vector3.Right),
        new(Keys.S, Vector3.Forward),
        new(Keys.D, Vector3.Left),
    ];

    public Keys ZoomOut { get; set; } = Keys.Z;
    public Keys ZoomIn { get; set; } = Keys.X;

    public override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();

        var linearSpeed = LinearSpeed;
        var zoomSpeed = ZoomSpeed;

        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
        {
            linearSpeed *= LinearSpeedFactor;
            zoomSpeed *= ZoomSpeedFactor;
        }

        var forward = Vector3.Transform(Vector3.Forward, _orthographicCamera.View.Rotation);
        forward.Y = 0;
        forward.Normalize();

        var angle = MathF.Atan2(forward.X, forward.Z);

        foreach (var (key, movementDirection) in MovementKeys)
        {
            if (keyboardState.IsKeyDown(key))
            {
                var direction = Vector3.Transform(movementDirection, Matrix.CreateRotationY(angle));

                _orthographicCamera.View.Position += linearSpeed * direction * (float)gameTime.ElapsedGameTime.TotalSeconds * _orthographicCamera.Projection.OrthographicSize;
            }
        }

        if (keyboardState.IsKeyDown(ZoomOut))
        {
            _orthographicCamera.Projection.OrthographicSize += linearSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds * zoomSpeed;
        }

        if (keyboardState.IsKeyDown(ZoomIn))
        {
            _orthographicCamera.Projection.OrthographicSize -= linearSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds * zoomSpeed;

            if (_orthographicCamera.Projection.OrthographicSize < 1)
            {
                _orthographicCamera.Projection.OrthographicSize = 1;
            }
        }
    }
}