using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;

namespace Olve.Engine3D.Rendering.Textures;

/// <summary>
/// Represents a texture in the domain layer.
/// AssetPath is optional - null for dynamically generated textures (heightmaps, render targets, etc.)
/// </summary>
public readonly record struct Texture(TextureData Data, AssetPath<TextureData>? AssetPath = null);