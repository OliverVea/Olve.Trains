using Silk.NET.Input;

namespace Olve.Engine3D.Input.InputSchemes;

public class WasdMovement : ICameraScheme
{
    private KeyboardState KeyboardState => GameManager.KeyboardManager.State;

    private static readonly (Key, Vector3D<float>)[] MovementDirections =
    [
        (Key.W, Vector3D<float>.UnitZ),
        (Key.S, -Vector3D<float>.UnitZ),
        (Key.A, Vector3D<float>.UnitX),
        (Key.D, -Vector3D<float>.UnitX),
        (Key.Space, Vector3D<float>.UnitY),
        (Key.ControlLeft, -Vector3D<float>.UnitY)
    ];

    private static readonly (Key, Vector2D<float>)[] RotationAxes =
    [
        (Key.Left, new Vector2D<float>(-1.0f, 0.0f)),
        (Key.Right, new Vector2D<float>(1.0f, 0.0f)),
        (Key.Up, new Vector2D<float>(0.0f, 1.0f)),
        (Key.Down, new Vector2D<float>(0.0f, -1.0f))
    ];

    private static readonly (Key, float)[] ZoomDirections =
    [
        (Key.Z, 1.0f),
        (Key.X, -1.0f)
    ];

    public CameraMovementInput GetMovementInput()
    {
        return new CameraMovementInput
        {
            Direction = GetDirection(),
            Rotation = GetRotation(),
            Zoom = GetZoom()
        };
    }

    private Vector3D<float> GetDirection()
    {
        Vector3D<float> direction = Vector3D<float>.Zero;

        foreach (var (key, value) in MovementDirections)
        {
            if (KeyboardState.IsKeyDown(key))
            {
                direction += value;
            }
        }

        return direction;
    }

    private Vector2D<float> GetRotation()
    {
        Vector2D<float> rotation = Vector2D<float>.Zero;

        foreach (var (key, value) in RotationAxes)
        {
            if (KeyboardState.IsKeyDown(key))
            {
                rotation += value;
            }
        }

        return rotation;
    }

    private float GetZoom()
    {
        float zoom = 0f;

        foreach (var (key, value) in ZoomDirections)
        {
            if (KeyboardState.IsKeyDown(key))
            {
                zoom = value;
            }
        }

        return zoom;
    }
}