using StrictId;

namespace Olve.Engine3D.Scenes;

public readonly record struct SceneId
{
    public Id<Scene> Value { get; }
    public string DisplayName { get; }

    public SceneId()
    {
        Value = Id<Scene>.NewId();
        DisplayName = GetDefaultDisplayName(Value);
    }

    public SceneId(Id<Scene> value) : this(value, GetDefaultDisplayName(value)) {}

    public SceneId(string displayName) : this(Id<Scene>.NewId(), displayName)
    {
    }

    public SceneId(Id<Scene> value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    private static string GetDefaultDisplayName(Id<Scene> value) => $"anonymous scene ({value.ToBase64()})";
}