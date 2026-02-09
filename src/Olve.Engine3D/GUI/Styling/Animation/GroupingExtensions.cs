namespace Olve.Engine3D.GUI.Styling.Animation;

public static class GroupingExtensions
{
    public static IEnumerable<(TKey, IEnumerable<TValue>)> Unpack<TKey, TValue>(
        this IEnumerable<IGrouping<TKey, TValue>> groupings)
    {
        return groupings.Select(x => (x.Key, x.AsEnumerable()));
    }
}