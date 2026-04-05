using Olve.Engine3D.Physics3D.Collisions;

namespace Olve.Trains.Scenes.GameLogic.Collision;

public static class ColliderGroups
{
    public static readonly Id<ColliderGroup> Signal = Id.FromName<ColliderGroup>("Signal");
    public static readonly Id<ColliderGroup> Building = Id.FromName<ColliderGroup>("Building");
    public static readonly Id<ColliderGroup> Train = Id.FromName<ColliderGroup>("Train");
    public static readonly Id<ColliderGroup> Track = Id.FromName<ColliderGroup>("Track");
    public static readonly Id<ColliderGroup> Terrain = Id.FromName<ColliderGroup>("Terrain");
    public static readonly Id<ColliderGroup> Environment = Id.FromName<ColliderGroup>("Environment");

    private static readonly HashSet<Id<ColliderGroup>> AutoClearableGroups = [Environment];

    /// <summary>
    /// Returns true if entities in this collider group are automatically removed
    /// when a building or track is placed on top of them (e.g. trees, mushrooms).
    /// </summary>
    public static bool IsAutoClearable(Id<ColliderGroup> group) => AutoClearableGroups.Contains(group);
}
