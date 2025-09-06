namespace Olve.Engine3D.Utilities;

public static class EnumerableExtensions
{
    public static bool IsInOrder<T>(this IEnumerable<T> enumerable)
    {
        var comparer = Comparer<T>.Default;
        using var enumerator = enumerable.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return true;
        }

        var previous = enumerator.Current;
        while (enumerator.MoveNext())
        {
            if (comparer.Compare(previous, enumerator.Current) > 0)
            {
                return false;
            }

            previous = enumerator.Current;
        }

        return true;
    }
}