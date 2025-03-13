using Engine.Camera.Cameras;
using Engine.Camera.Projections;
using Engine.Camera.Views;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Camera.Controllers;

public class IsometricCameraController(IsometricOrthographicCamera orthographicCamera) : CameraControllerBase<IsometricOrthographicCamera>(orthographicCamera)
{
    public static IsometricCameraController Create(
        Vector3 initialTarget,
        Vector3 viewDirection,
        float orthographicSize,
        float aspectRatio,
        float nearPlane,
        float farPlane)
    {
        viewDirection = Vector3.Normalize(viewDirection);

        var cameraPosition = initialTarget + viewDirection;

        var cameraRotationMatrix = Matrix.CreateLookAt(cameraPosition, initialTarget, Vector3.Up);
        var cameraRotation = Quaternion.CreateFromRotationMatrix(cameraRotationMatrix);

        var view = new IsometricView
        {
            Position = cameraPosition,
            Rotation = cameraRotation
        };

        var projection = new OrthographicProjection
        {
            OrthographicSize = orthographicSize,
            AspectRatio = aspectRatio,
            NearPlane = nearPlane,
            FarPlane = farPlane
        };

        var camera = new IsometricOrthographicCamera(view, projection);

        return new IsometricCameraController(camera);
    }

    private readonly IsometricOrthographicCamera _orthographicCamera = orthographicCamera;
    public float LinearSpeed { get; set; } = 2.0f;
    public float LinearSpeedFactor { get; set; } = 3.0f;

    public float ZoomSpeed { get; set; } = 50.0f;
    public float ZoomSpeedFactor { get; set; } = 3.0f;

    public List<KeyDirection> MovementKeys { get; init; } =
    [
        new(Keys.W, Vector3.Backward),
        new(Keys.A, Vector3.Right),
        new(Keys.S, Vector3.Forward),
        new(Keys.D, Vector3.Left),
    ];

    public Keys RotateClockwise { get; set; } = Keys.Q;
    public Keys RotateCounterClockwise { get; set; } = Keys.E;
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

        var angle = MathF.Atan2(forward.X, forward.Z) + _orthographicCamera.View.Angle;

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

        if (keyboardState.IsKeyDown(RotateClockwise))
        {
            _orthographicCamera.View.Angle += linearSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        if (keyboardState.IsKeyDown(RotateCounterClockwise))
        {
            _orthographicCamera.View.Angle -= linearSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}