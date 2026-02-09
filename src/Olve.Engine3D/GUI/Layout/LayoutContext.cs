namespace Olve.Engine3D.GUI.Layout;

public readonly record struct LayoutContext(
    Vector2D<Dp> DesignSize,
    float AspectRatio,
    DpPxRatio DpPxRatio,
    float UiScale)
{
    public Dp ToDp(Px px) => DpPxRatio.ToDp(px);
    public Px ToPx(Dp dp) => DpPxRatio.ToPx(dp);

    public Vector2D<Dp> ToDp(Vector2D<Px> px) => new(DpPxRatio.ToDp(px.X), DpPxRatio.ToDp(px.Y));
    public Vector2D<Px> ToPx(Vector2D<Dp> dp) => new(DpPxRatio.ToPx(dp.X), DpPxRatio.ToPx(dp.Y));

    public Vector3D<Dp> ToDp(Vector3D<Px> px) =>
        new(DpPxRatio.ToDp(px.X), DpPxRatio.ToDp(px.Y), DpPxRatio.ToDp(px.Z));

    public Vector3D<Px> ToPx(Vector3D<Dp> dp) =>
        new(DpPxRatio.ToPx(dp.X), DpPxRatio.ToPx(dp.Y), DpPxRatio.ToPx(dp.Z));

    public Vector4D<Dp> ToDp(Vector4D<Px> px) =>
        new(DpPxRatio.ToDp(px.X), DpPxRatio.ToDp(px.Y), DpPxRatio.ToDp(px.Z), DpPxRatio.ToDp(px.W));

    public Vector4D<Px> ToPx(Vector4D<Dp> dp) =>
        new(DpPxRatio.ToPx(dp.X), DpPxRatio.ToPx(dp.Y), DpPxRatio.ToPx(dp.Z), DpPxRatio.ToPx(dp.W));
}