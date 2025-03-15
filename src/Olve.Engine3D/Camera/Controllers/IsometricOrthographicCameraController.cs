using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Olve.Engine3D.Camera.Cameras;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Core;

namespace Olve.Engine3D.Camera.Controllers;

public class IsometricPerspectiveCameraController(IsometricPerspectiveCamera camera)
    : CameraControllerBase<IsometricPerspectiveCamera>(camera)
{
    public static IsometricPerspectiveCameraController Create(
        Vector3 initialTarget,
        Vector3 viewDirection,
        float initialDistance,
        float fieldOfView,
        float aspectRatio,
        float nearPlane,
        float farPlane)
    {
        viewDirection = Vector3.Normalize(viewDirection);

        var cameraPosition = initialTarget - viewDirection * initialDistance;

        var cameraRotationMatrix = Matrix.CreateLookAt(cameraPosition, initialTarget, Vector3.Up);
        var cameraRotation = Quaternion.CreateFromRotationMatrix(cameraRotationMatrix);

        var view = new IsometricView
        {
            Position = cameraPosition,
            Rotation = cameraRotation
        };

        var projection = new PerspectiveProjection
        {
            FieldOfView = fieldOfView,
            AspectRatio = aspectRatio,
            NearPlane = nearPlane,
            FarPlane = farPlane
        };

        var camera = new IsometricPerspectiveCamera(view, projection);
        return new IsometricPerspectiveCameraController(camera);
    }

    private readonly IsometricPerspectiveCamera _camera = camera;

    public float LinearSpeed { get; set; } = 20.0f;

    public float AngularSpeed { get; set; } = MathHelper.PiOver2;
    public float ZoomSpeed { get; set; } = 50.0f;
    public float SpeedFactor { get; set; } = 3.0f;

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

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var speedUp = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);

        var linearSpeed = speedUp ? LinearSpeed * SpeedFactor : LinearSpeed;
        var zoomSpeed = speedUp ? ZoomSpeed * SpeedFactor : ZoomSpeed;
        var angularSpeed = speedUp ? AngularSpeed * SpeedFactor : AngularSpeed;

        var forward = Vector3.Transform(Vector3.Forward, _camera.View.Rotation);
        forward.Y = 0;
        forward.Normalize();

        var angle = MathF.Atan2(forward.X, forward.Z) + _camera.View.Angle;

        // Handle movement
        foreach (var (key, movementDirection) in MovementKeys)
        {
            if (keyboardState.IsKeyDown(key))
            {
                var direction = Vector3.Transform(movementDirection, Matrix.CreateRotationY(angle));
                _camera.View.Position += direction * linearSpeed * deltaTime;
            }
        }

        // Handle zoom (move along the view direction)
        var viewDirection = Vector3.Transform(Vector3.Forward, _camera.View.Rotation);
        if (keyboardState.IsKeyDown(ZoomIn))
        {
            _camera.View.Position -= viewDirection * zoomSpeed * deltaTime;
        }

        if (keyboardState.IsKeyDown(ZoomOut))
        {
            _camera.View.Position += viewDirection * zoomSpeed * deltaTime;
        }

        // Handle rotation
        if (keyboardState.IsKeyDown(RotateClockwise))
        {
            _camera.View.Angle += angularSpeed * deltaTime;
        }

        if (keyboardState.IsKeyDown(RotateCounterClockwise))
        {
            _camera.View.Angle -= angularSpeed * deltaTime;
        }
    }
}

public class IsometricOrthographicCameraController(IsometricOrthographicCamera camera) : CameraControllerBase<IsometricOrthographicCamera>(camera)
{
    public static IsometricOrthographicCameraController Create(
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

        return new IsometricOrthographicCameraController(camera);
    }

    private readonly IsometricOrthographicCamera _camera = camera;
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

        var forward = Vector3.Transform(Vector3.Forward, _camera.View.Rotation);
        forward.Y = 0;
        forward.Normalize();

        var angle = MathF.Atan2(forward.X, forward.Z) + _camera.View.Angle;

        foreach (var (key, movementDirection) in MovementKeys)
        {
            if (keyboardState.IsKeyDown(key))
            {
                var direction = Vector3.Transform(movementDirection, Matrix.CreateRotationY(angle));

                _camera.View.Position += linearSpeed * direction * (float)gameTime.ElapsedGameTime.TotalSeconds * _camera.Projection.OrthographicSize;
            }
        }

        if (keyboardState.IsKeyDown(ZoomOut))
        {
            _camera.Projection.OrthographicSize += linearSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds * zoomSpeed;
        }

        if (keyboardState.IsKeyDown(ZoomIn))
        {
            _camera.Projection.OrthographicSize -= linearSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds * zoomSpeed;

            if (_camera.Projection.OrthographicSize < 1)
            {
                _camera.Projection.OrthographicSize = 1;
            }
        }

        if (keyboardState.IsKeyDown(RotateClockwise))
        {
            _camera.View.Angle += linearSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        if (keyboardState.IsKeyDown(RotateCounterClockwise))
        {
            _camera.View.Angle -= linearSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}