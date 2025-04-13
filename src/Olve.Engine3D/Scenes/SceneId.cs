using System.Diagnostics;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

[DebuggerDisplay("{DisplayName}")]
public readonly record struct SceneId
{
    public Id<Scene> Value { get; }
    public string DisplayName { get; }

    public SceneId()
    {
        Value = Id<Scene>.New();
        DisplayName = GetDefaultDisplayName(Value);
    }

    public SceneId(Id<Scene> value) : this(value, GetDefaultDisplayName(value)) {}

    public SceneId(string displayName) : this(Id<Scene>.New(), displayName)
    {
    }

    public SceneId(Id<Scene> value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    private static string GetDefaultDisplayName(Id<Scene> value) => $"anonymous scene ({value})";
}