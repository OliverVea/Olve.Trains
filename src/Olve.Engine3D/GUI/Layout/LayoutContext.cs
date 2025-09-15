namespace Olve.Engine3D.GUI.Layout;

public readonly record struct LayoutContext(
    Vector2D<int> ViewportSize,
    Vector2D<int> DesignSize,
    float DevicePixelRatio,
    float UiScale)
{
    public float FitScale => float.Min(
        (float)ViewportSize.X / DesignSize.X,
        (float)ViewportSize.Y / DesignSize.Y);

    public float CanvasToScreen => FitScale * DevicePixelRatio;
    public float DpToPx => UiScale * CanvasToScreen;
}