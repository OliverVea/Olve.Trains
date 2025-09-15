namespace Olve.Engine3D.GUI.Layout;

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
    
    public static Dp operator+(Dp left, Dp right) => new(left.Value + right.Value);
    public static Dp operator-(Dp left, Dp right) => new(left.Value - right.Value);
    public static Dp operator*(int left, Dp right) => new(right.Value * left);
    public static Dp operator*(float left, Dp right) => new(right.Value * left);
    public static bool operator<(Dp left, Dp right) => left.Value < right.Value;
    public static bool operator >(Dp left, Dp right) => left.Value > right.Value;

    public static Dp Zero { get; } = new(0);

    public static Dp Max(Dp left, Dp right) => left.Value > right.Value ? left : right;
    public static Dp Min(Dp left, Dp right) => left.Value > right.Value ? right : left;
}