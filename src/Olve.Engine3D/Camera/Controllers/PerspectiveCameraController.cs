using Olve.Engine3D.Camera.Cameras;

namespace Olve.Engine3D.Camera.Controllers;

public class PerspectiveCameraController(PerspectiveCamera camera) : CameraControllerBase<PerspectiveCamera>(camera)
{
    private readonly PerspectiveCamera _camera = camera;
    public float LinearSpeed { get; set; } = 10.0f;
    public float AngularSpeed { get; set; } = PiOver2;
    public float ZoomSpeed { get; set; } = 5f;

    private Vector3D<float> Forward => Vector3D.Transform(Vector3D<float>.UnitZ, _camera.View.Rotation);

    public override void Move(Vector3D<float> direction, TimeSpan deltaTime, float scale = 1f)
    {
        if (direction.LengthSquared < 0.1f)
        {
            return;
        }

        direction = Vector3D.Normalize(direction) * scale;

        var rotatedDirection = Vector3D.Transform(direction, _camera.View.Rotation);
        _camera.View.Position += rotatedDirection * LinearSpeed * deltaTime.InSeconds();
    }

    public override void Rotate(Vector2D<float> rotation, TimeSpan deltaTime, float scale = 1f)
    {
        if (rotation.LengthSquared < 0.0000001f)
        {
            return;
        }

        rotation = Vector2D.Normalize(rotation) * scale;

        var yaw = -rotation.X * AngularSpeed * deltaTime.InSeconds();
        var pitch = -rotation.Y * AngularSpeed * deltaTime.InSeconds();

        var right = Vector3D.Transform(Vector3D<float>.UnitX, _camera.View.Rotation);
        var yawRotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, yaw);
        var pitchRotation = Quaternion<float>.CreateFromAxisAngle(right, pitch);

        _camera.View.Rotation = Quaternion<float>.Normalize(pitchRotation * yawRotation * _camera.View.Rotation);
    }

    public override void Zoom(float delta, TimeSpan deltaTime, float scale = 1f)
    {
        if (MathF.Abs(delta) < 0.1f)
        {
            return;
        }

        delta = float.Clamp(delta, -1f, 1f);

        var zoomAmount = float.Pow(ZoomSpeed, delta) * scale * deltaTime.InSeconds();

        _camera.Projection.FieldOfView = Math.Clamp(
            _camera.Projection.FieldOfView - zoomAmount,
            MathF.PI / 6,
            MathF.PI / 2
        );
    }
}