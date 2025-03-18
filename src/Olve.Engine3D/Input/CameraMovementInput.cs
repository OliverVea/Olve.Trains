namespace Olve.Engine3D.Input;

public readonly record struct CameraMovementInput()
{
    public Vector3D<float> Direction { get; init; } = Vector3D<float>.Zero;
    public Vector2D<float> Rotation { get; init; } = Vector2D<float>.Zero;
    public float Zoom { get; init; } = 0f;

    public static CameraMovementInput operator+(CameraMovementInput a, CameraMovementInput b) => new()
    {
        Direction = a.Direction + b.Direction,
        Rotation = a.Rotation + b.Rotation,
        Zoom = a.Zoom + b.Zoom
    };
}