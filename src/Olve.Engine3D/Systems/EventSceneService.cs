using Olve.Engine3D.Scenes;

namespace Olve.Engine3D.Systems;

public sealed class EventSceneService<T>(EventQueue<T> queue) : ISceneService
{
    public Result Load()
    {
        queue.Init();
        return Result.Success();
    }

    public Result Unload()
    {
        queue.Cleanup();
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime) => queue.Update();
}
