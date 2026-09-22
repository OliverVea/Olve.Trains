using Microsoft.Extensions.DependencyInjection;

namespace Olve.Engine3D.Scenes;

public static class SceneArgumentsExtensions
{
    public static SceneArguments With<TParameters>(this SceneKey<TParameters> sceneKey, TParameters parameters) =>
        new(sceneKey.Id, provider => provider
            .GetKeyedServices<ISceneParameterService<TParameters>>(sceneKey.Id)
            .Select(service => service.LoadParameters(parameters))
            .ToArray()
            .TryPickProblems(out var problems) ? problems : Result.Success());
}
