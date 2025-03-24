namespace Olve.Engine3D.Assets;

public readonly record struct AssetPath<T>(string Name, string Path)
{
    public static implicit operator string(AssetPath<T> path) => path.Path;
}