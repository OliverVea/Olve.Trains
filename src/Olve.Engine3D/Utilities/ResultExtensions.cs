namespace Olve.Engine3D.Utilities;

public static class ResultExtensions
{
    public static Result ToEmptyResult<T>(this Result<T> result) => result.TryPickProblems(out var problems) 
        ? problems
        : Result.Success();

    public static Result<TDestination> MapValue<TSource, TDestination>(this Result<TSource> result, Func<TSource, Result<TDestination>> mapper)
    {
        if (result.TryPickProblems(out var problems, out var value))
        {
            return problems;
        }

        return mapper(value);
    }
    
    public static Result<TDestination> MapValue<TSource, TDestination>(this Result<TSource> result, Func<TSource, TDestination> mapper)
    {
        if (result.TryPickProblems(out var problems, out var value))
        {
            return problems;
        }

        return mapper(value);
    }
}