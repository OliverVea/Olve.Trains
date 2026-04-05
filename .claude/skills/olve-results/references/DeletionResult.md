# DeletionResult

https://olivervea.github.io/Olve.Utilities/api/Olve.Results.DeletionResult.html

Readonly struct. Three-state result for deletion operations: success, not found, or error.

Properties:
- `bool Succeeded` — deletion succeeded
- `bool Failed` — not-found or error
- `bool WasNotFound` — entity was not found
- `ResultProblemCollection? Problems` — null unless error

Static methods:
- `DeletionResult Success()` — successful deletion
- `DeletionResult NotFound()` — entity not found
- `DeletionResult Error(params IEnumerable<ResultProblem> problems)` — deletion failed

Instance methods:
- `bool TryPickProblems(out ResultProblemCollection? problems)` — true if error state

Implicit conversions: ResultProblem -> DeletionResult (error), ResultProblemCollection -> DeletionResult (error)

Extension methods (DeletionResultExtensions):
- `Result MapToResult(bool allowNotFound = true)` — converts to Result. Not-found treated as success by default.
- `T Match<T>(Func<T> onSuccess, Func<T> onNotFound, Func<ResultProblemCollection, T> onProblems)` — exhaustive pattern match
