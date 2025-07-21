using System;
using System.Collections.Generic;

namespace Olve.Trains.AssetPipeline;

public static class S3OptionsParser
{
    public static (TimeSpan Timeout, bool AllowFailure, string[] RemainingArgs) Parse(string[] args)
    {
        TimeSpan timeout = TimeSpan.FromMilliseconds(200);
        bool allowFailure = false;

        List<string> remaining = new(args);
        for (int i = 0; i < remaining.Count; )
        {
            var arg = remaining[i];
            if (arg == "--s3-timeout" && i + 1 < remaining.Count && int.TryParse(remaining[i + 1], out var ms))
            {
                timeout = TimeSpan.FromMilliseconds(ms);
                remaining.RemoveAt(i);
                remaining.RemoveAt(i);
                continue;
            }
            if (arg == "--allow-s3-failure")
            {
                allowFailure = true;
                remaining.RemoveAt(i);
                continue;
            }

            i++;
        }

        return (timeout, allowFailure, remaining.ToArray());
    }
}
