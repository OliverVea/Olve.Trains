# Result\<T\>

https://olivervea.github.io/Olve.Utilities/api/Olve.Results.Result-1.html

Readonly struct. Success with a value or failure.

Properties:
- `bool Succeeded`
- `bool Failed`
- `T? Value`
- `ResultProblemCollection? Problems`

Instance methods:
- `bool TryPickProblems(out ResultProblemCollection? problems)` — true if failed
- `bool TryPickProblems(out ResultProblemCollection? problems, out T? value)` — true if failed; always populates both
- `bool TryPickValue(out T? value)` — true if succeeded
- `bool TryPickValue(out T? value, out ResultProblemCollection? problems)` — true if succeeded; always populates both
- `T GetValueOrDefault(T defaultValue)`
- `bool TryGetValueOrDefault(out T? value, T defaultValue)`
- `Result<T> IfProblem(Action<ResultProblemCollection> action)` — execute on failure, returns self

Implicit conversions: T -> Result<T>, ResultProblem -> Result<T>, ResultProblemCollection -> Result<T>

Extension methods:
- `Result<TDest> Map<TDest>(Func<T, TDest> map)` — transform value. Returns original problems on failure.
- `Result<TDest> Bind<TDest>(Func<T, Result<TDest>> bind)` — transform with Result-returning func. Returns original problems on failure.
- `Result ToEmptyResult()` — discard value, keep success/failure.
