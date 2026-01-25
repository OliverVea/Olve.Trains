namespace Olve.Engine3D.GUI.Layout;

public readonly record struct AnchorPosition(
    HorizontalPosition Horizontal,
    VerticalPosition Vertical)
{
    public static readonly AnchorPosition TopLeft = new(HorizontalPosition.Left, VerticalPosition.Top);
    public static readonly AnchorPosition TopCenter = new(HorizontalPosition.Center, VerticalPosition.Top);
    public static readonly AnchorPosition TopRight = new(HorizontalPosition.Right, VerticalPosition.Top);
    public static readonly AnchorPosition MiddleLeft = new(HorizontalPosition.Left, VerticalPosition.Middle);
    public static readonly AnchorPosition Center = new(HorizontalPosition.Center, VerticalPosition.Middle);
    public static readonly AnchorPosition MiddleRight = new(HorizontalPosition.Right, VerticalPosition.Middle);
    public static readonly AnchorPosition BottomLeft = new(HorizontalPosition.Left, VerticalPosition.Bottom);
    public static readonly AnchorPosition BottomCenter = new(HorizontalPosition.Center, VerticalPosition.Bottom);
    public static readonly AnchorPosition BottomRight = new(HorizontalPosition.Right, VerticalPosition.Bottom);

    public Vector2D<float> ToNormalized() => new(
        Horizontal switch {
            HorizontalPosition.Left => 0f,
            HorizontalPosition.Center => 0.5f,
            HorizontalPosition.Right => 1f,
            _ => throw new ArgumentOutOfRangeException()
        },
        Vertical switch {
            VerticalPosition.Top => 0f,
            VerticalPosition.Middle => 0.5f,
            VerticalPosition.Bottom => 1f,
            _ => throw new ArgumentOutOfRangeException()
        }
    );
}
