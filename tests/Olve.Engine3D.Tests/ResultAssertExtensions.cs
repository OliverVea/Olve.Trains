using Olve.Results;
using Olve.Results.TUnit;

namespace Olve.Engine3D.Tests;

internal static class ResultAssertExtensions
{
    public static async Task<T> AssertSuccessAndGetAsync<T>(this Result<T> result)
    {
        await Assert.That(result).Succeeded();
        return result.Value!;
    }

    public static async Task<T> AssertSuccessAndGetAsync<T>(this Task<Result<T>> resultTask)
    {
        var result = await resultTask;
        await Assert.That(result).Succeeded();
        return result.Value!;
    }

    public static async Task AssertSuccessAsync(this Result result)
        => await Assert.That(result).Succeeded();

    public static async Task AssertSuccessAsync(this Task<Result> resultTask)
    {
        var result = await resultTask;
        await Assert.That(result).Succeeded();
    }
}