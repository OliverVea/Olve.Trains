using Olve.Engine3D.Scenes;
using Olve.Results;

namespace Olve.Trains.Scenes;

public interface ISceneService
{
    Result Load();
    Result Update(TimeSpan deltaTime);
    Result<Pass> Input() => Pass.Pass;
    Result Unload();
}