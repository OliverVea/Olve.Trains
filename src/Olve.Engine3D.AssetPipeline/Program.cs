using Olve.Engine3D.AssetPipeline.S3;
using Olve.Results;

var result = await Main();

if (result.TryPickProblems(out var mainProblems))
{
    foreach (var problem in mainProblems)
    {
        Console.WriteLine(problem);
    }

    return 1;
}

Console.WriteLine("Finished processing assets!");

return 0;


async Task<Result> Main()
{
    var result = await DownloadAssetsToTemp.ExecuteAsync();
    if (result.TryPickProblems(out var problems))
    {
        return problems;
    }

    return Result.Success();
}

