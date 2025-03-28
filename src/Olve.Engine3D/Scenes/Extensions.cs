namespace Olve.Engine3D.Scenes;

public static class Extensions
{
    public static IEnumerable<Func<Result>> MapResult<T>(this IEnumerable<T> source, Func<T, Result> selector)
    {
        return source.Select(item => new Func<Result>(() => selector(item)));
    }
}