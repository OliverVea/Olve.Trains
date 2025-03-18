using Olve.Engine3D.Camera.Cameras;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Input;

namespace Olve.Engine3D.Camera.Controllers;

/*
public class IsometricPerspectiveCameraController(IsometricPerspectiveCamera camera)
    : CameraControllerBase<IsometricPerspectiveCamera>(camera)
{
    public static IsometricPerspectiveCameraController Create(
        Vector3D<float> initialTarget,
        Vector3D<float> viewDirection,
        float initialDistance,
        float fieldOfView,
        float aspectRatio,
        float nearPlane,
        float farPlane)
    {
        viewDirection = Vector3D.Normalize(viewDirection);

        var cameraPosition = initialTarget - viewDirection * initialDistance;

        var cameraRotationMatrix = Matrix4X4.CreateLookAt(cameraPosition, initialTarget, Vector3D<float>.UnitY);
        var cameraRotation = Quaternion<float>.CreateFromRotationMatrix(cameraRotationMatrix);

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

    public float AngularSpeed { get; set; } = PiOver4;
    public float ZoomSpeed { get; set; } = 50.0f;
    public float SpeedFactor { get; set; } = 3.0f;

    public List<KeyDirection> MovementKeys { get; init; } =
    [
        new(Keys.W, -Vector3D<float>.UnitZ),
        new(Keys.A, -Vector3D<float>.UnitX),
        new(Keys.S, Vector3D<float>.UnitZ),
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

        var speedUp = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);

        var linearSpeed = speedUp ? LinearSpeed * SpeedFactor : LinearSpeed;
        var zoomSpeed = speedUp ? ZoomSpeed * SpeedFactor : ZoomSpeed;
        var angularSpeed = speedUp ? AngularSpeed * SpeedFactor : AngularSpeed;

        var forward = Vector3D.Transform(Vector3D<float>.UnitZ, _camera.View.Rotation);
        forward.Y = 0;
        forward = Vector3D.Normalize(forward);

        var angle = MathF.Atan2(forward.X, forward.Z) + _camera.View.Angle;

        // Handle movement
        foreach (var (key, movementDirection) in MovementKeys)
        {
            if (keyboardState.IsKeyDown(key))
            {
                var direction = Vector3D.Transform(movementDirection, Matrix4X4.CreateRotationY(angle));
                _camera.View.Position += direction * linearSpeed * deltaSeconds;
            }
        }

        // Handle zoom (move along the view direction)
        var viewDirection = Vector3D.Transform(Vector3D<float>.UnitZ, _camera.View.Rotation);
        if (keyboardState.IsKeyDown(ZoomIn))
        {
            _camera.View.Position -= viewDirection * zoomSpeed *deltaSeconds;
        }

        if (keyboardState.IsKeyDown(ZoomOut))
        {
            _camera.View.Position += viewDirection * zoomSpeed * deltaSeconds;
        }

        // Handle rotation
        if (keyboardState.IsKeyDown(RotateClockwise))
        {
            _camera.View.Angle += angularSpeed * deltaSeconds;
        }

        if (keyboardState.IsKeyDown(RotateCounterClockwise))
        {
            _camera.View.Angle -= angularSpeed * deltaSeconds;
        }
    }
}
*/