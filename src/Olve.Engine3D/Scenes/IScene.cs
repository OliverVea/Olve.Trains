using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public interface IScene
{
    /// <summary>
    /// Scene state, managed by <see cref="SceneManager"/>
    /// </summary>
    SceneState State { get; set; }
    Result Load();
    Result Unload();
    Result Update(TimeSpan deltaTime);
    Result<Pass> Input(TimeSpan deltaTime);
    Result Render(TimeSpan deltaTime);
    Id<IScene> Id { get; }
    SceneLayer Layer { get; }
    int LayerOrder { get; }
}