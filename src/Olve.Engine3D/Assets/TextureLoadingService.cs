using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Logging;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Assets;

public class TextureLoadingService(
    ILoggingManager loggingManager,
    TextureManager textureManager,
    TextureRenderingManager textureRenderingManager) : SceneService(loggingManager)
{
    private static readonly Vector4D<byte> White = new(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly TextureData SingleWhitePixelData = new() { Height = 1, Width = 1, Pixels = [White] };

    private Id<Texture>? _fallbackTexture;

    public Id<Texture> SingleWhitePixelId { get; private set; }


    protected override Result OnLoad()
    {
        SingleWhitePixelId = textureManager.RegisterDynamicTexture(SingleWhitePixelData, "SingleWhitePixel");
        return textureRenderingManager.EnsureTextureLoaded(SingleWhitePixelId).ToEmptyResult();
    }

    public void SetFallbackTexture(Id<Texture>? fallbackTexture)
    {
        _fallbackTexture = fallbackTexture;
    }

    /// <summary>
    /// Ensures the texture is loaded and registered for rendering.
    /// Returns the texture Id for use in shader parameters.
    /// If texturePath is null, uses the fallback texture.
    /// </summary>
    public Result<Id<Texture>> LoadTextureOrFallbackIfNull(AssetPath<TextureData>? texturePath)
    {
        if (texturePath is not {} path)
        {
            return _fallbackTexture ?? SingleWhitePixelId;
        }

        // Load the texture data
        if (textureManager.EnsureTextureLoaded(path).TryPickProblems(out var problems, out var textureId))
        {
            return problems.Prepend("Failed to load texture: {0}", path);
        }

        // Ensure the texture is registered for rendering (uploads to OpenGL)
        if (textureRenderingManager.EnsureTextureLoaded(textureId).TryPickProblems(out problems, out _))
        {
            return problems.Prepend("Failed to register texture for rendering: {0}", path);
        }

        return textureId;
    }

    /// <summary>
    /// Loads a texture from an asset path and returns its Id.
    /// </summary>
    public Result<Id<Texture>> LoadTexture(AssetPath<TextureData> texturePath)
    {
        if (textureManager.EnsureTextureLoaded(texturePath).TryPickProblems(out var problems, out var textureId))
        {
            return problems.Prepend("Failed to load texture: {0}", texturePath);
        }

        if (textureRenderingManager.EnsureTextureLoaded(textureId).TryPickProblems(out problems, out _))
        {
            return problems.Prepend("Failed to register texture for rendering: {0}", texturePath);
        }

        return textureId;
    }
}