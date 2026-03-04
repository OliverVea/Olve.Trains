using Olve.Engine3D.Physics3D.Collisions;

namespace Olve.Trains.Scenes.GameLogic;

public static class ColliderGroups
{
    public static readonly Id<ColliderGroup> Signal = Id.FromName<ColliderGroup>("Signal");
    public static readonly Id<ColliderGroup> Building = Id.FromName<ColliderGroup>("Building");
    public static readonly Id<ColliderGroup> Vehicle = Id.FromName<ColliderGroup>("Vehicle");
    public static readonly Id<ColliderGroup> Track = Id.FromName<ColliderGroup>("Track");
    public static readonly Id<ColliderGroup> Terrain = Id.FromName<ColliderGroup>("Terrain");
}
