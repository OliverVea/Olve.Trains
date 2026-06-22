using Microsoft.Extensions.Logging;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Meshes;
using Olve.Generated.Meshes;
using Olve.Generated.Textures;

namespace Olve.Trains.Scenes.Loading;

/// <summary>
/// Pre-loads every generated mesh and texture into the singleton CPU asset
/// caches so the game scene's <c>Load()</c> hits the cache instead of reading
/// from disk. Safe to call from a background thread — the loading managers
/// guard their caches with locks. Failures are logged and skipped: pre-warming
/// is an optimization, and any genuinely broken asset surfaces again when the
/// game scene tries to use it.
/// </summary>
public class AssetPrewarmService(
    MeshLoadingManager meshLoadingManager,
    TextureLoadingManager textureLoadingManager,
    ILogger<AssetPrewarmService> logger)
{
    public void PrewarmAll()
    {
        var meshCount = 0;
        foreach (var mesh in Meshes.All)
        {
            if (meshLoadingManager.LoadMesh(mesh).TryPickProblems(out var problems, out _))
            {
                logger.LogWarning("Failed to pre-warm mesh '{Mesh}': {Problem}", mesh.Name, problems.First().Message);
                continue;
            }

            meshCount++;
        }

        var textureCount = 0;
        foreach (var texture in Textures.All)
        {
            if (textureLoadingManager.LoadTexture(texture).TryPickProblems(out var problems, out _))
            {
                logger.LogWarning("Failed to pre-warm texture '{Texture}': {Problem}", texture.Name, problems.First().Message);
                continue;
            }

            textureCount++;
        }

        logger.LogInformation("Pre-warmed {MeshCount} mesh(es) and {TextureCount} texture(s)", meshCount, textureCount);
    }
}
