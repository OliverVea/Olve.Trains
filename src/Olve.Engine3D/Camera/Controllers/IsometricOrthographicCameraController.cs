using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Utilities;
using ControllerCamera = Olve.Engine3D.Camera.Camera<Olve.Engine3D.Camera.Views.IsometricView, Olve.Engine3D.Camera.Projections.OrthographicProjection>;

namespace Olve.Engine3D.Camera.Controllers;

public class IsometricOrthographicCameraController(ControllerCamera camera) : CameraControllerBase<ControllerCamera>(camera)
{
    private readonly ControllerCamera _camera = camera;
    public float MoveSpeed { get; set; } = 2.0f;
    public float ZoomSpeed { get; set; } = 50f;

    public override void Move(Vector3D<float> direction, TimeSpan deltaTime, float scale = 1f)
    {
        if (direction.LengthSquared < 0.1f)
        {
            return;
        }

        var cameraToWorld = Matrix4X4.Transpose(_camera.View.Rotation);
        var worldDirection = Vector3D.Transform(direction, cameraToWorld);

        worldDirection.Y = 0;
        worldDirection = Vector3D.Normalize(-worldDirection);

        var movement = worldDirection * MoveSpeed * deltaTime.InSeconds() * _camera.Projection.OrthographicSize;

        _camera.View.Position += movement;
    }


    public override void Rotate(Vector2D<float> rotation, TimeSpan deltaTime, float scale = 1f)
    {
        throw new NotSupportedException();
    }

    public override void Zoom(float delta, TimeSpan deltaTime, float scale = 1f)
    {
        if (MathF.Abs(delta) < 0.1f)
        {
            return;
        }

        delta = float.Clamp(delta, -1f, 1f);

        var zoomAmount = ZoomSpeed * delta * scale * deltaTime.InSeconds();
        _camera.Projection.OrthographicSize = float.Max(1f, _camera.Projection.OrthographicSize - zoomAmount);
    }

    public static IsometricOrthographicCameraController Create(
        Vector2D<float> windowSize,
        Vector3D<float> target,
        Vector3D<float> viewDirection,
        float orthographicSize = 10f)
    {
        var cameraPosition = target + Vector3D.Normalize(viewDirection);

        var forward = Vector3D.Normalize(target - cameraPosition);
        var right = Vector3D.Normalize(Vector3D.Cross(Vector3D<float>.UnitY, forward));
        var up = Vector3D.Cross(forward, right);

        var worldRotation = new Matrix4X4<float>(
            right.X,   up.X,   forward.X,  0,
            right.Y,   up.Y,   forward.Y,  0,
            right.Z,   up.Z,   forward.Z,  0,
            0,         0,      0,          1);

        IsometricView view = new() { Position = cameraPosition, Rotation = worldRotation };

        var aspectRatio = windowSize.X / windowSize.Y;
        const float nearPlane = 0.01f;
        const float farPlane = 10000f;

        OrthographicProjection projection = new() { AspectRatio = aspectRatio, NearPlane = nearPlane, FarPlane = farPlane, OrthographicSize = orthographicSize};

        var camera = Olve.Engine3D.Camera.Camera.Create(view, projection);

        return new IsometricOrthographicCameraController(camera);
    }
}