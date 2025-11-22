using Olve.Paths;

namespace Olve.Trains.AssetPipeline.Assets;

public static class PathExtensions
{
    public static bool EnsurePathExists(this IPath path)
    {
        if (path.Exists())
        {
            return false;
        }

        Directory.CreateDirectory(path.Path);

        return true;
    }
}