using Microsoft.Extensions.Logging;

namespace Olve.Engine3D.Utilities;

public static class LoggerExtensions
{
    public static void Log<T>(this ILogger<T> logger, ResultProblemCollection problems)
    {
        // TODO: implement this in a better way :)
        foreach (var problem in problems)
        {
            var message = $"{problem.Message} at {{{problem.Args.Length}}}";

            var problemArgs = problem.Args.Append(problem.OriginInformation.LinkString).ToArray();

            logger.LogError(message, problemArgs);
        }
    }
}