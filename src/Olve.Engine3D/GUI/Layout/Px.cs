namespace Olve.Engine3D.GUI.Layout;

public readonly record struct Px(int Value) : IFormattable, IComparable<Px>
{
    public static readonly Px Zero = new(0);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        FormattableString formattable = $"{nameof(Value)}: {Value}";
        return formattable.ToString(formatProvider);
    }

    public override string ToString()
    {
        return $"{nameof(Value)}: {Value}";
    }

    public int CompareTo(Px other)
    {
        return Value.CompareTo(other.Value);
    }

    public static implicit operator Px(int value) => new(value);

    public static Px operator+(Px a, Px b) => new(a.Value + b.Value);
    public static Px operator-(Px a, Px b) => new(a.Value - b.Value);

    public static bool operator <=(Px a, Px b) => a.Value <= b.Value;
    public static bool operator >=(Px a, Px b) => a.Value >= b.Value;
}