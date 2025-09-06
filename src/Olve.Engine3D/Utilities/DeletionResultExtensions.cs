namespace Olve.Engine3D.Utilities;

public static class DeletionResultExtensions
{
    public static T Map<T>(this DeletionResult result, Func<T> onSuccess, Func<T> onNotFound, Func<ResultProblemCollection, T> onProblems)
    {
        if (result.Problems is { } problems)
        {
            return onProblems(problems);
        }
        
        if (result.Succeeded)
        {
            return onSuccess();
        }

        if (result.WasNotFound)
        {
            return onNotFound();
        }

        ResultProblemCollection resultProblems = new(new ResultProblem("Deletion result was invalid"));
        return onProblems(resultProblems);
    }

    public static Result MapToResult(this DeletionResult result, bool allowNotFound = true)
    {
        if (result.Problems is { } problems)
        {
            return problems;
        }
        
        if (result.Succeeded)
        {
            return Result.Success();
        }

        if (result.WasNotFound)
        {
            return allowNotFound 
                ? Result.Success()
                : new ResultProblem("Did not find required entity to delete");
        }

        return new ResultProblemCollection(new ResultProblem("Deletion result was invalid"));
    }
}