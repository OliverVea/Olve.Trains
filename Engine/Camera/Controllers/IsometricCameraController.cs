using Engine.Camera.Cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Camera.Controllers;

public class IsometricCameraController(IsometricCamera camera) : CameraControllerBase<IsometricCamera>(camera)
{
    private readonly IsometricCamera _camera = camera;
    public float LinearSpeed { get; set; } = 10.0f;
    public float LinearSpeedFactor { get; set; } = 2.0f;

    public List<KeyDirection> MovementKeys { get; init; } =
    [
        new(Keys.W, Vector3.Forward),
        new(Keys.A, Vector3.Left),
        new(Keys.S, Vector3.Backward),
        new(Keys.D, Vector3.Right),
    ];

    public List<KeyRotation> RotationKeys { get; init; } =
    [
    ];

    public override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();

        var linearSpeed = LinearSpeed;

        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
        {
            linearSpeed *= LinearSpeedFactor;
        }

        // Handle movement inputs
        foreach (var (key, movementDirection) in MovementKeys)
        {
            if (keyboardState.IsKeyDown(key))
            {
                var direction = Vector3.Transform(movementDirection, _camera.View.Rotation);
                _camera.View.Position += linearSpeed * direction * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }

        // Handle rotation inputs (though in an isometric camera, this is usually minimal)
        foreach (var (key, rotation) in RotationKeys)
        {
            if (keyboardState.IsKeyDown(key))
            {
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                // Incremental rotation around the Y-axis
                Quaternion incrementalRotation = Quaternion.Slerp(Quaternion.Identity, rotation, deltaTime);

                // Apply the incremental rotation
                _camera.View.Rotation = Quaternion.Normalize(Quaternion.Concatenate(_camera.View.Rotation, incrementalRotation));
            }
        }
    }
}