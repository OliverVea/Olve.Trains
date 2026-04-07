using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.Loading;

public class LoadingSceneParameterService : ISceneParameterService<LoadingSceneArguments>
{
    public LoadingSceneArguments Arguments { get; private set; } = new();

    public Result LoadParameters(LoadingSceneArguments parameters)
    {
        Arguments = parameters;
        return Result.Success();
    }
}
