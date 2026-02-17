using System.Diagnostics.CodeAnalysis;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Buildings;

public abstract class AbstractBlueprintPropertiesService<T>
{
    private readonly Dictionary<Id<BuildingBlueprint>, T> _blueprintProperties = [];

    public Event<Id<BuildingBlueprint>> PropertiesChanged { get; } = new();

    public void SetProperties(Id<BuildingBlueprint> blueprintId, T properties)
    {
        if (TryGetProperties(blueprintId, out var existingProperties)
            && existingProperties!.Equals(properties))
        {
            return;
        }

        _blueprintProperties[blueprintId] = properties;
        PropertiesChanged.Invoke(blueprintId);
    }

    public void ClearProperties(Id<BuildingBlueprint> blueprintId)
    {
        if (_blueprintProperties.Remove(blueprintId))
        {
            PropertiesChanged.Invoke(blueprintId);
        }
    }

    public bool HasProperties(Id<BuildingBlueprint> blueprintId) =>
        _blueprintProperties.ContainsKey(blueprintId);

    public bool TryGetProperties(Id<BuildingBlueprint> blueprintId, [MaybeNullWhen(false)] out T properties) =>
        _blueprintProperties.TryGetValue(blueprintId, out properties);
}