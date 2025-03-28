using Olve.Results;

namespace Olve.Trains.Scenes;

public interface ISceneService
{
    Result Load();
    Result Update(TimeSpan deltaTime);
    Result Unload();
}