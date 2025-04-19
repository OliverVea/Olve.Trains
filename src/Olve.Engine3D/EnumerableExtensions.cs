namespace Olve.Engine3D;

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
    
    public static bool CollectionEquals<T>(this IReadOnlyList<T>? first, IReadOnlyList<T>? second)
        where T : notnull
    {
        if (first is null && second is null)
        {
            return true;
        }

        if (first is null || second is null)
        {
            return false;
        }

        if (first.Count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Count; i++)
        {
            if (!first[i].Equals(second[i]))
            {
                return false;
            }
        }

        return true;
    }
}