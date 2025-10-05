namespace Olve.Trains.AssetPipeline;

public static class BuildTargetParser
{
    public static BuildTargets Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return BuildTargets.All;
        }

        BuildTargets targets = BuildTargets.None;
        foreach (var arg in args)
        {
            var normalized = arg.TrimStart('-').ToLowerInvariant();
            var parsed = normalized switch
            {
                "shaders" => BuildTargets.Shaders,
                "meshes" => BuildTargets.Meshes,
                "textures" => BuildTargets.Textures,
                "terrains" => BuildTargets.Terrains,
                "layouts" => BuildTargets.Layouts,
                "all" => BuildTargets.All,
                _ => BuildTargets.None
            };

            if (parsed == BuildTargets.All)
            {
                return BuildTargets.All;
            }

            targets |= parsed;
        }

        return targets == BuildTargets.None ? BuildTargets.All : targets;
    }
}
