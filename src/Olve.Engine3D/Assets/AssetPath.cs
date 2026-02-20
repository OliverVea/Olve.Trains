namespace Olve.Engine3D.Assets;

public readonly record struct AssetPath<T>
{
    public string Name { get; }
    public IPath Path { get; }

    public AssetPath(string name, string path)
    {
        Name = name;
        Path = Paths.Path.Create(path);
    }

    public static implicit operator string(AssetPath<T> path) => path.Path.Path;
}