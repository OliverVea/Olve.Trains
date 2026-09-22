using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public readonly record struct SceneKey<TParameters>(Id<IScene> Id);
