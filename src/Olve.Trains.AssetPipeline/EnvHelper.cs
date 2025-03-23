using Olve.Results;

namespace Olve.Engine3D.AssetPipeline;

public static class EnvHelper
{
    public static Result<string> ReadEnvVariable(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (value == null)
        {
            return new ResultProblem("Environment variable '{0}' is not set", key);
        }

        return value;
    }
}