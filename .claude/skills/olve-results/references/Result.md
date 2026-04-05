# Result

https://olivervea.github.io/Olve.Utilities/api/Olve.Results.Result.html

Readonly struct. Success or failure without a value.

Properties:
- `bool Succeeded`
- `bool Failed`
- `ResultProblemCollection? Problems`

Static methods:
- `Result Success()`
- `Result<T> Success<T>(T value)`
- `Result Failure(params IEnumerable<ResultProblem> problems)`
- `Result<T> Failure<T>(params IEnumerable<ResultProblem> problems)`
- `Result Try<TException>(Action action, string? message = null, params object[] args)` — catches TException, returns as problems
- `Result<TValue> Try<TValue, TException>(Func<TValue> action, string? message = null, params object[] args)`

Chain (sequential dependent steps, stops on first failure):
- `Result Chain(params IEnumerable<Func<Result>> links)`
- `Result<T2> Chain<T1, T2>(Func<Result<T1>>, Func<T1, Result<T2>>)`
- `Result<T3> Chain<T1, T2, T3>(...)`
- `Result<T4> Chain<T1, T2, T3, T4>(...)`

Concat (independent steps, aggregates all problems):
- `Result Concat(params IEnumerable<Func<Result>> elements)`
- `Result Concat(params IEnumerable<Result> results)`
- `Result<(T1, T2)> Concat<T1, T2>(...)` through `Result<(T1..T6)> Concat<T1..T6>(...)`
- Both direct Result<T> and Func<Result<T>> overloads for each arity.

Instance methods:
- `bool TryPickProblems(out ResultProblemCollection? problems)` — true if failed
- `Result IfProblem(Action<ResultProblemCollection> action)` — execute on failure, returns self

Implicit conversions: ResultProblem -> Result, ResultProblemCollection -> Result
