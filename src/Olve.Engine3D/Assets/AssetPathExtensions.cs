using Olve.Engine3D.Graphics.Shaders;

namespace Olve.Engine3D.Assets;


public abstract class Asset;

public abstract class TextAsset : Asset;
public abstract class ShaderAsset : TextAsset;
public abstract class FragmentShaderAsset : ShaderAsset;
public abstract class VertexShaderAsset : ShaderAsset;


public static class AssetPathExtensions
{
    public static Result<string> ReadTextAsset<T>(this AssetPath<T> assetPath)
        where T : TextAsset
    {
        var absolutePath = Path.GetFullPath(assetPath.Path);

        if (!File.Exists(absolutePath))
        {
            return new ResultProblem("No file was found at '{0}' (resolve into '{1}').", assetPath.Path, absolutePath);
        }

        return File.ReadAllText(absolutePath);
    }

    public static Result<ShaderSource<T>> ReadShader<T>(this AssetPath<T> assetPath) where T : ShaderAsset
    {
        if (assetPath.ReadTextAsset().TryPickProblems(out var problems, out var sourceCode))
        {
            return problems;
        }

        return new ShaderSource<T>(assetPath, sourceCode);
    }

}