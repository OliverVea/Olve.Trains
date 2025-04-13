using MemoryPack;
using Olve.Paths;

namespace Olve.Engine3D.Assets;

public static class AssetLoader
{
    public static readonly IPath AssetFolder = Paths.Path.Create("assets");

    private static IPath GetAssetLocation<T>(AssetPath<T> assetPath) => Olve.Paths.Path.TryGetAssemblyExecutable(out var assemblyFile)
        ? assemblyFile.Parent / AssetFolder / assetPath.Path
        : throw new InvalidOperationException("Could not get assembly executable path");

    public static Result<T> LoadAsset<T>(AssetPath<T> assetPath)
    {
        var location = GetAssetLocation(assetPath);

        if (!location.Exists())
        {
            return new ResultProblem("Attempted to load asset '{0}' of type '{1}' at non-existent path '{2}'",
                assetPath.Name,
                typeof(T).Name,
                location.Path);
        }

        var bytes = File.ReadAllBytes(location.Path);

        try
        {
            var mesh = MemoryPackSerializer.Deserialize<T>(bytes);
            if (mesh is null)
            {
                return new ResultProblem("Could not read asset '{0}' of type '{1}' at path '{2}'",
                    assetPath.Name,
                    typeof(T).Name,
                    location.Path);
            }

            return mesh;
        }
        catch (Exception e)
        {
            return new ResultProblem(e, "Got unexpected error while reading asset '{0}' of type '{1}': ",
                location,
                typeof(T).Name);
        }
    }
}