namespace Olve.Trains.AssetPipeline;

public static class ShadersDirParser
{
    /// <summary>
    /// Parses --shaders-dir option from args.
    /// Returns (shadersDir, remainingArgs).
    /// Supports both "--shaders-dir=value" and "--shaders-dir value".
    /// </summary>
    public static (string? ShadersDir, string[] RemainingArgs) Parse(string[] args)
    {
        string? shadersDir = null;

        List<string> remaining = new(args);
        for (var i = 0; i < remaining.Count; )
        {
            var arg = remaining[i];
            if (arg.StartsWith("--shaders-dir=", StringComparison.Ordinal))
            {
                shadersDir = arg.Substring("--shaders-dir=".Length);
                remaining.RemoveAt(i);
                continue;
            }

            if (arg == "--shaders-dir")
            {
                if (i + 1 < remaining.Count)
                {
                    shadersDir = remaining[i + 1];
                    remaining.RemoveAt(i + 1);
                }

                remaining.RemoveAt(i);
                continue;
            }

            i++;
        }

        return (shadersDir, remaining.ToArray());
    }
}
