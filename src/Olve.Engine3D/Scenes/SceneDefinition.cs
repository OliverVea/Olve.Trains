using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public sealed record SceneDefinition(
    Id<IScene> Id,
    string Name,
    int LayerOrder = 0,
    Id<IScene>? ParentId = null);
