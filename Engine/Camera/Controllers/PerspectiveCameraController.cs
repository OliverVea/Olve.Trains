using Engine.Camera.Cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Camera.Controllers;

public class PerspectiveCameraController(PerspectiveCamera camera) : CameraControllerBase<PerspectiveCamera>(camera)
{
    private readonly PerspectiveCamera _camera = camera;
    public float LinearSpeed { get; set; } = 10.0f;
    public float AngularSpeed { get; set; } = MathHelper.PiOver2;
    public float LinearSpeedFactor { get; set; } = 2.0f;
    public float AngularSpeedFactor { get; set; } = 2.0f;

    public List<KeyDirection> MovementKeys { get; init; } =
    [
        new(Keys.W, Vector3.Forward),
        new(Keys.A, Vector3.Left),
        new(Keys.S, Vector3.Backward),
        new(Keys.D, Vector3.Right),
    ];

    public List<KeyRotation> RotationKeys { get; init; } =
    [
        new (Keys.Q, Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll(1, 0, 0))),
        new (Keys.E, Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll(-1, 0, 0))),
    ];

    public override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();

        var linearSpeed = LinearSpeed;
        var angularSpeed = AngularSpeed;

        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
        {
            linearSpeed *= LinearSpeedFactor;
            angularSpeed *= AngularSpeedFactor;
        }

        foreach (var (key, movementDirection) in MovementKeys)
        {
            if (keyboardState.IsKeyDown(key))
            {
                var direction = Vector3.Transform(movementDirection, _camera.View.Rotation);

                _camera.View.Position += linearSpeed * direction * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }

        foreach (var (key, rotation) in RotationKeys)
        {
            if (keyboardState.IsKeyDown(key))
            {
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                // Create a small incremental rotation: from no rotation (Identity) to the target rotation.
                Quaternion incrementalRotation = Quaternion.Slerp(Quaternion.Identity, rotation, deltaTime * angularSpeed);

                // Apply the incremental rotation to the current camera rotation.
                _camera.View.Rotation = Quaternion.Normalize(
                    Quaternion.Concatenate(_camera.View.Rotation, incrementalRotation)
                );
            }
        }
    }
}