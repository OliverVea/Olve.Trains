namespace Olve.Engine3D.Utilities;

public static class DictionaryResultExtensions
{
    public static Result SetWithResult<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : notnull
    {
        if (!dictionary.TryAdd(key, value))
        {
            return new ResultProblem("Key '{0}' already exists", key);
        }

        return Result.Success();
    }
    
    public static Result<TValue> GetWithResult<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull
    {
        return dictionary.TryGetValue(key, out var value)
            ? Result.Success(value)
            : new ResultProblem("Could not find value for key '{0}'", key);
    }
}