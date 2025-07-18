namespace Olve.Trains.AssetPipeline;

[Flags]
public enum BuildTargets
{
    None = 0,
    Shaders = 1 << 0,
    Meshes = 1 << 1,
    Textures = 1 << 2,
    Terrains = 1 << 3,
    All = Shaders | Meshes | Textures | Terrains
}

public static class BuildTargetExtensions
{
    public static bool RequiresS3Resources(this BuildTargets targets)
        => (targets & (BuildTargets.Meshes | BuildTargets.Textures | BuildTargets.Terrains)) != 0;
}
