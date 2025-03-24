using MemoryPack;

namespace Olve.Engine3D.Assets;

public static class AssetLoader
{
    public const string AssetFolder = "assets";

    private static string GetAssetLocation<T>(AssetPath<T> assetPath) => $"{AssetFolder}/{assetPath.Path}";

    public static Result<T> LoadAsset<T>(AssetPath<T> assetPath)
    {
        var location = GetAssetLocation(assetPath);

        if (!File.Exists(location))
        {
            return new ResultProblem("Attempted to load asset '{0}' of type '{1}' at non-existent path '{2}'",
                assetPath.Name,
                typeof(T).Name,
                location);
        }

        var bytes = File.ReadAllBytes(location);

        try
        {
            var mesh = MemoryPackSerializer.Deserialize<T>(bytes);
            if (mesh is null)
            {
                return new ResultProblem("Could not read asset '{0}' of type '{1}' at path '{2}'",
                    assetPath.Name,
                    typeof(T).Name,
                    location);
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