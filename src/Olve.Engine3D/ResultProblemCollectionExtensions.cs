namespace Olve.Engine3D;

public static class ResultProblemCollectionExtensions
{
    public static ResultProblemCollection Log(this ResultProblemCollection resultProblemCollection)
    {
        foreach (var problem in resultProblemCollection)
        {
            Console.WriteLine(problem.ToDebugString());
        }

        return resultProblemCollection;
    }

    public static Exception ToException(this ResultProblemCollection resultProblemCollection)
    {
        return new Exception();
    }
}