# Result Extension Classes

## ResultExtensions

https://olivervea.github.io/Olve.Utilities/api/Olve.Results.ResultExtensions.html

- `Result ToEmptyResult<T>(this Result<T> result)` — discard value, keep success/failure
- `Result<TDest> Map<TSource, TDest>(this Result<TSource> result, Func<TSource, TDest> map)` — transform value. Returns original problems on failure.
- `Result<TDest> Bind<TSource, TDest>(this Result<TSource> result, Func<TSource, Result<TDest>> bind)` — transform with Result-returning func. Returns original problems on failure.
- `Result<T> WithValueOnSuccess<T>(this Result result, T value)` — attach value to successful Result

## ResultEnumerableExtensions

https://olivervea.github.io/Olve.Utilities/api/Olve.Results.ResultEnumerableExtensions.html

IEnumerable<Result>:
- `bool TryPickProblems(out ResultProblemCollection? problems)` — true if any failed, aggregates all
- `bool HasProblems()` — true if any failed
- `IEnumerable<ResultProblem> GetProblems()` — flattens all problems

IEnumerable<Result<T>>:
- `bool TryPickProblems(out ResultProblemCollection? problems, out IReadOnlyList<T> values)` — true if any failed, populates both
- `bool HasProblems()` — true if any failed
- `IEnumerable<T> GetValues()` — successful values only
- `IEnumerable<ResultProblem> GetProblems()` — flattens all problems

## DictionaryResultExtensions

https://olivervea.github.io/Olve.Utilities/api/Olve.Results.DictionaryResultExtensions.html

- `Result SetWithResult<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)` — fails if key exists
- `Result<TValue> GetWithResult<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict, TKey key)` — fails if key missing
