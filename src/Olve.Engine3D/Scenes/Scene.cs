namespace Olve.Engine3D.Scenes;

public abstract class Scene
{
    public abstract SceneId Id { get; }
    public virtual SceneLayer Layer => SceneLayer.Main;
    /// <summary>
    /// Scene state, managed by <see cref="SceneManager"/>
    /// </summary>
    public SceneState State { get; set; }

    public virtual int LayerOrder => 0;

    public virtual Result Load() => Result.Success();
    public virtual void Unload() { }
    public virtual Result<Pass> Input() => Pass.Pass;
    public virtual Result Update(TimeSpan deltaTime) => Result.Success();
    public virtual Result Render(TimeSpan deltaTime) => Result.Success();
}