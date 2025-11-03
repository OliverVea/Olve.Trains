namespace Olve.Trains.AssetPipeline;

public static class BuildTargetExtensions
{
    public static bool RequiresS3Resources(this BuildTargets targets)
        => (targets & (BuildTargets.Meshes | BuildTargets.Textures | BuildTargets.Terrains | BuildTargets.Fonts)) != 0;
}