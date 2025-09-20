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
}