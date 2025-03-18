using Olve.Engine3D.Camera.Cameras;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Input;

namespace Olve.Engine3D.Camera.Controllers;

/*
public class IsometricOrthographicCameraController(IsometricOrthographicCamera camera) : CameraControllerBase<IsometricOrthographicCamera>(camera)
{
    public static IsometricOrthographicCameraController Create(
        Vector3D<float> initialTarget,
        Vector3D<float> viewDirection,
        float orthographicSize,
        float aspectRatio,
        float nearPlane,
        float farPlane)
    {
        viewDirection = Vector3D.Normalize(viewDirection);

        var cameraPosition = initialTarget + viewDirection;

        var cameraRotationMatrix = Matrix4X4.CreateLookAt(cameraPosition, initialTarget, Vector3D<float>.UnitY);
        var cameraRotation = Quaternion<float>.CreateFromRotationMatrix(cameraRotationMatrix);

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
        new(Keys.W, Vector3D<float>.UnitZ),
        new(Keys.A, -Vector3D<float>.UnitX),
        new(Keys.S, -Vector3D<float>.UnitZ),
        new(Keys.D, Vector3D<float>.UnitX),
    ];

    public Keys RotateClockwise { get; set; } = Keys.Q;
    public Keys RotateCounterClockwise { get; set; } = Keys.E;
    public Keys ZoomOut { get; set; } = Keys.Z;
    public Keys ZoomIn { get; set; } = Keys.X;

    public override void Update(TimeSpan deltaTime)
    {
        var deltaSeconds = (float)deltaTime.TotalSeconds;
        var keyboardState = Keyboard.GetState();

        var linearSpeed = LinearSpeed;
        var zoomSpeed = ZoomSpeed;

        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
        {
            linearSpeed *= LinearSpeedFactor;
            zoomSpeed *= ZoomSpeedFactor;
        }

        var forward = Vector3D.Transform(Vector3D<float>.UnitZ, _camera.View.Rotation);
        forward.Y = 0;
        forward = Vector3D.Normalize(forward);

        var angle = MathF.Atan2(forward.X, forward.Z) + _camera.View.Angle;

        foreach (var (key, movementDirection) in MovementKeys)
        {
            if (keyboardState.IsKeyDown(key))
            {
                var direction = Vector3D.Transform(movementDirection, Matrix4X4.CreateRotationY(angle));

                _camera.View.Position += linearSpeed * direction * deltaSeconds * _camera.Projection.OrthographicSize;
            }
        }

        if (keyboardState.IsKeyDown(ZoomOut))
        {
            _camera.Projection.OrthographicSize += linearSpeed * deltaSeconds * zoomSpeed;
        }

        if (keyboardState.IsKeyDown(ZoomIn))
        {
            _camera.Projection.OrthographicSize -= linearSpeed * deltaSeconds * zoomSpeed;

            if (_camera.Projection.OrthographicSize < 1)
            {
                _camera.Projection.OrthographicSize = 1;
            }
        }

        if (keyboardState.IsKeyDown(RotateClockwise))
        {
            _camera.View.Angle += linearSpeed * deltaSeconds;
        }

        if (keyboardState.IsKeyDown(RotateCounterClockwise))
        {
            _camera.View.Angle -= linearSpeed * deltaSeconds;
        }
    }
}
*/