using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public readonly record struct SceneArguments(Id<IScene> SceneId, Func<IServiceProvider, Result> Apply);
