namespace Olve.Trains.Scenes.Console;

public static class EnumerableExtensions
{
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