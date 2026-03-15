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
    Result Update();
    Result<Pass> Input();
    Result Render();
    Id<IScene> Id { get; }
    SceneLayer Layer { get; }
    int LayerOrder { get; }
}
