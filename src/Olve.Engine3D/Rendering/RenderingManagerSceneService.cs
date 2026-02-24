using Olve.Engine3D.Scenes;

namespace Olve.Engine3D.Rendering;

/// <summary>
/// Scene service that calls <see cref="RenderingManager.RenderAll"/> once per frame.
/// Should run after all other rendering services (high priority value).
/// </summary>
public class RenderingManagerSceneService(RenderingManager renderingManager) : ISceneService
{
    public int Priority => int.MaxValue;

    public Result Render(TimeSpan deltaTime)
    {
        return renderingManager.RenderAll();
    }
}
