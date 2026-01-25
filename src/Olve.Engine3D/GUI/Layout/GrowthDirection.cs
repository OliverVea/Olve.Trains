namespace Olve.Engine3D.GUI.Layout;

public readonly record struct GrowthDirection(
    HorizontalGrowth Horizontal,
    VerticalGrowth Vertical)
{
    public static readonly GrowthDirection DownRight = new(HorizontalGrowth.Right, VerticalGrowth.Down);
    public static readonly GrowthDirection UpRight = new(HorizontalGrowth.Right, VerticalGrowth.Up);
    public static readonly GrowthDirection DownLeft = new(HorizontalGrowth.Left, VerticalGrowth.Down);
    public static readonly GrowthDirection UpLeft = new(HorizontalGrowth.Left, VerticalGrowth.Up);
    public static readonly GrowthDirection Up = new(HorizontalGrowth.Both, VerticalGrowth.Up);
    public static readonly GrowthDirection Down = new(HorizontalGrowth.Both, VerticalGrowth.Down);
    public static readonly GrowthDirection Left = new(HorizontalGrowth.Left, VerticalGrowth.Both);
    public static readonly GrowthDirection Right = new(HorizontalGrowth.Right, VerticalGrowth.Both);
    public static readonly GrowthDirection Center = new(HorizontalGrowth.Both, VerticalGrowth.Both);
}
