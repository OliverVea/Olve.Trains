namespace Olve.Trains.AssetPipeline;

public static class LayoutsOptionsParser
{
    /// <summary>
    /// Parses layout-related CLI options from args.
    /// Supports:
    ///   --layouts-dir <path>       or --layouts-dir=<path>
    ///   --layouts-namespace <ns>    or --layouts-namespace=<ns>
    /// Returns (layoutsDir, layoutsNamespace, remainingArgs).
    /// </summary>
    public static (string? LayoutsDir, string? Namespace, string[] RemainingArgs) Parse(string[] args)
    {
        string? layoutsDir = null;
        string? ns = null;

        List<string> remaining = new(args);
        for (var i = 0; i < remaining.Count; )
        {
            var arg = remaining[i];

            if (arg.StartsWith("--layouts-dir=", StringComparison.Ordinal))
            {
                layoutsDir = arg.Substring("--layouts-dir=".Length);
                remaining.RemoveAt(i);
                continue;
            }

            if (arg == "--layouts-dir")
            {
                if (i + 1 < remaining.Count)
                {
                    layoutsDir = remaining[i + 1];
                    remaining.RemoveAt(i + 1);
                }

                remaining.RemoveAt(i);
                continue;
            }

            if (arg.StartsWith("--layouts-namespace=", StringComparison.Ordinal))
            {
                ns = arg.Substring("--layouts-namespace=".Length);
                remaining.RemoveAt(i);
                continue;
            }

            if (arg == "--layouts-namespace")
            {
                if (i + 1 < remaining.Count)
                {
                    ns = remaining[i + 1];
                    remaining.RemoveAt(i + 1);
                }

                remaining.RemoveAt(i);
                continue;
            }

            i++;
        }

        return (layoutsDir, ns, remaining.ToArray());
    }
}
