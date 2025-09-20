namespace Olve.Engine3D.GUI.Layout;

public static class DpEnumerableExtensions
{
    public static Dp Sum(this IEnumerable<Dp> enumerable)
    {
        return enumerable.Aggregate(Dp.Zero, (a, b) => a + b);
    }
    
    public static Dp Sum<T>(this IEnumerable<T> enumerable, Func<T, Dp> selector)
    {
        return enumerable.Aggregate(Dp.Zero, (a, b) => a + selector(b));
    }
    
    public static Dp Max<T>(this IEnumerable<T> enumerable, Func<T, Dp> selector)
    {
        return enumerable.Aggregate(Dp.Zero, (a, b) => Dp.Max(a, selector(b)));
    }
    
    public static Dp Min<T>(this IEnumerable<T> enumerable, Func<T, Dp> selector)
    {
        return enumerable.Aggregate(Dp.Zero, (a, b) => Dp.Min(a, selector(b)));
    }
}