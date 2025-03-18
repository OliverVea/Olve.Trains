using StrictId;

namespace Olve.Engine3D.Graphics;

public readonly record struct EntityId
{
    public Id<Entity> Value { get; }
    public string DisplayName { get; }

    public EntityId()
    {
        Value = Id<Entity>.NewId();
        DisplayName = GetDefaultDisplayName(Value);
    }

    public EntityId(Id<Entity> value) : this(value, GetDefaultDisplayName(value)) {}

    public EntityId(string displayName) : this(Id<Entity>.NewId(), displayName)
    {
    }

    public EntityId(Id<Entity> value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    private static string GetDefaultDisplayName(Id<Entity> value) => $"anonymous entity ({value.ToBase64()})";
}