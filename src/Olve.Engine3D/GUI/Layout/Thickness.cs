namespace Olve.Engine3D.GUI.Layout;

public readonly record struct Thickness(Dp Left, Dp Top, Dp Right, Dp Bottom)
{
    public static Thickness Zero { get; } = All(Dp.Zero);
    public static Thickness All(Dp length) => new(length, length, length, length);
    public static Thickness Sym(Dp xLength, Dp yLength) => new(xLength, yLength, xLength, yLength);

    public Dp Horizontal => Left + Right;
    public Dp Vertical => Top + Bottom;
}