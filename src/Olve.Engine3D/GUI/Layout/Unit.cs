using OneOf;

namespace Olve.Engine3D.GUI.Layout;

public enum UIAxis { X, Y }

public readonly record struct Pct(float Value);
public readonly record struct Dp(float Value) : IFormattable, IComparable<Dp>
{
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        FormattableString formattable = $"{nameof(Value)}: {Value}";
        return formattable.ToString(formatProvider);
    }

    public override string ToString()
    {
        return $"{nameof(Value)}: {Value}";
    }

    public int CompareTo(Dp other)
    {
        return Value.CompareTo(other.Value);
    }
}


public readonly record struct Length
{
    private enum LengthUnit { Dp, Pct }
    
    private readonly LengthUnit _lengthUnit;
    private readonly float _value;

    private Length(float value, LengthUnit lengthUnit)
    {
        _lengthUnit = lengthUnit;
        _value = value;
    }

    public static Length From(Dp dp) => new(dp.Value, LengthUnit.Dp);
    public static Length From(Pct pct) => new(pct.Value, LengthUnit.Pct);
    
    public static implicit operator Length(Dp dp) => From(dp); 
    public static implicit operator Length(Pct pct) => From(pct); 

    public T Match<T>(Func<Dp, T> dpAction, Func<Pct, T> pctAction)
    {
        return _lengthUnit switch
        {
            LengthUnit.Dp => dpAction(new Dp(_value)),
            LengthUnit.Pct => pctAction(new Pct(_value)),
            _ => throw new NotSupportedException($"Length has unsupported unit '{_lengthUnit}'")
        };
    }
}

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

public static class LengthExtensions
{
    public static T GetForAxis<T>(this Vector2D<T> vector, UIAxis axis) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        return axis switch
        {
            UIAxis.X => vector.X,
            UIAxis.Y => vector.Y,
            _ => throw new ArgumentOutOfRangeException(nameof(axis), $"Could not parse ui axis {axis}")
        };
    }
    
    public static float ResolveToDp(this Length length, Vector2D<Dp> parentSize, UIAxis axis)
    {
        return length.Match(
            dp => dp.Value,
            pct => parentSize.GetForAxis<Dp>(axis).Value * pct.Value * 0.01f
        );
    }

    public static float ResolveToPx(this Length l, Vector2D<Dp> parentSize, UIAxis axis, LayoutContext ctx)
    {
        var dp = l.ResolveToDp(parentSize, axis);
        var px = dp * ctx.DpToPx;

        return float.Round(px);
    }
    
    public static (float leftPx, float topPx, float rightPx, float bottomPx) ResolveToPx(this Thickness t, Vector2D<Dp> parentSize, LayoutContext ctx)
        => ( t.Left.ResolveToPx(parentSize, UIAxis.X, ctx),
            t.Top.ResolveToPx(parentSize, UIAxis.Y, ctx),
            t.Right.ResolveToPx(parentSize, UIAxis.X, ctx),
            t.Bottom.ResolveToPx(parentSize, UIAxis.Y, ctx) );
}

public readonly record struct Thickness(Length Left, Length Top, Length Right, Length Bottom)
{
    public static Thickness All(Length length) => new(length, length, length, length);
    public static Thickness Sym(Length xLength, Length yLength) => new(xLength, yLength, xLength, yLength);
}

public readonly record struct Color(float R, float G, float B, float A);

public readonly record struct Border(Thickness Width, Color Color);

public enum LayoutMode { Absolute, FlowRow, FlowColumn }
public enum Align { Start, Center, End, Stretch }
public enum Justify { Start, Center, End, SpaceBetween }

public readonly record struct SizeSpec(Length? Width = null, Length? Height = null);