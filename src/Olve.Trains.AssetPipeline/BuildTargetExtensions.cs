namespace Olve.Trains.AssetPipeline;

public static class BuildTargetExtensions
{
    public static bool RequiresSourceAssets(this BuildTargets targets)
        => (targets & (BuildTargets.Meshes | BuildTargets.Textures | BuildTargets.Terrains | BuildTargets.Fonts | BuildTargets.TextureAtlases)) != 0;
}