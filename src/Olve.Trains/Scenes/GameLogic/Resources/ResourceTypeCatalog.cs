namespace Olve.Trains.Scenes.GameLogic.Resources;

public static class ResourceTypeCatalog
{
    public static Id<ResourceType> Wood { get; } = Id.FromName<ResourceType>("resource/wood");
    public static Id<ResourceType> Ore { get; } = Id.FromName<ResourceType>("resource/ore");
}
