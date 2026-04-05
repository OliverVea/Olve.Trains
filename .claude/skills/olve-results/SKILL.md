---
name: olve-results
description: Reference for Olve.Results — Result/Result<T>, TryPickProblems, ResultProblem, DeletionResult, Chain/Concat/Map/Bind composition, and error propagation with Prepend. Use when writing or reading code that uses the Result pattern.
user-invocable: false
---

# Olve.Results

Error handling via `Result` types. Source: `Olve.Utilities/src/Olve.Results/`.

Reference docs: [README](references/README.md) | [Result](references/Result.md) | [Result\<T\>](references/Result_T.md) | [ResultProblem](references/ResultProblem.md) | [ResultProblemCollection](references/ResultProblemCollection.md) | [DeletionResult](references/DeletionResult.md) | [Extensions](references/Extensions.md)

**Golden rule:** Always use `TryPickProblems`, never `.Failed`/`.Succeeded`/`.Value` directly.

## Result (valueless)

```csharp
// Success
return Result.Success();

// Failure — implicit conversion from ResultProblem
return new ResultProblem("Something failed");

// Check
if (DoSomething().TryPickProblems(out var problems))
{
    return problems.Prepend("Context about what failed");
}
```

## Result\<T\> (valued)

```csharp
// Success — implicit conversion from T
return myValue;

// Failure
return new ResultProblem("Not found: '{0}'", id);

// Check — value is safe to use after the if block
if (GetValue().TryPickProblems(out var problems, out var value))
{
    return problems;
}
// use value here
```

## ResultProblem

**Use format strings with `{0}`, `{1}` — NOT string interpolation:**

```csharp
// Correct
return new ResultProblem("Failed to parse '{0}' as {1}", input, typeName);

// WRONG — loses structured args
return new ResultProblem($"Failed to parse '{input}'");

// With exception
return new ResultProblem(exception, "Failed to read '{0}'", path);
```

Implicit conversion: `ResultProblem` converts directly to `Result` or `Result<T>`.

## DeletionResult

Three states for delete operations:

```csharp
return DeletionResult.Success();    // deleted
return DeletionResult.NotFound();   // entity didn't exist
return DeletionResult.Error(problems);  // something went wrong
```

```csharp
// Check
if (result.TryPickProblems(out var problems)) { ... }  // true on error only
if (result.WasNotFound) { ... }

// Convert to Result (not-found treated as success by default)
Result asResult = deletionResult.MapToResult();

// Pattern match
string msg = deletionResult.Match(
    onSuccess: () => "Deleted",
    onNotFound: () => "Already gone",
    onProblems: p => p.First().ToBriefString());
```

## Composition

### Chain — sequential dependent steps, short-circuits on first failure

```csharp
// Valueless
Result.Chain(() => Step1(), () => Step2());

// With values — each step receives the previous value
Result<T2> result = Result.Chain(
    () => GetA(),       // Result<T1>
    a => Process(a));   // T1 -> Result<T2>

// Up to 4 steps
Result<T3> result = Result.Chain(
    () => GetA(), a => GetB(a), b => GetC(b));
```

### Concat — independent steps, aggregates all problems

```csharp
// Valueless — runs all, collects all errors
Result.Concat(() => Validate1(), () => Validate2(), () => Validate3());

// With values — returns tuple on success
Result<(GL, IInputContext)> = Result.Concat(
    () => CreateGL(), () => CreateInput());
```

### Map — transform value (non-Result function)

```csharp
Result<int> length = GetString().Map(s => s.Length);
```

### Bind — transform value (Result-returning function)

```csharp
Result<int> parsed = GetString().Bind(s =>
    int.TryParse(s, out var n)
        ? Result.Success(n)
        : new ResultProblem("Invalid int: '{0}'", s));
```

### ToEmptyResult — discard value

```csharp
Result empty = GetValue().ToEmptyResult();  // Result<T> -> Result
```

## Error Context with Prepend

Build hierarchical error traces by prepending context as errors propagate up:

```csharp
if (LoadMesh(path).TryPickProblems(out var problems, out var mesh))
{
    return problems.Prepend("Failed to load building mesh for collision");
}

// Result is: "Failed to load building mesh for collision" → "File not found: '{0}'"
```

## Collection Extensions

```csharp
IEnumerable<Result<T>> results = items.Select(Process);

// Aggregate — true if any failed, collects all problems + all values
if (results.TryPickProblems(out var problems, out var values))
{
    return problems;
}
```

## Result.Try — exception wrapping

```csharp
Result<GL> gl = Result.Try<GL, Exception>(
    () => window.CreateOpenGL(),
    "Error creating OpenGL context");
```
