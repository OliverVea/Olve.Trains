using Olve.Engine3D.Assets;

namespace Olve.Engine3D.Graphics.Shaders;

public readonly record struct ShaderSource<T>(AssetPath<T> Path, string SourceCode) where T : ShaderAsset;