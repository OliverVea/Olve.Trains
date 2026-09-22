using Microsoft.Extensions.DependencyInjection;

namespace Olve.Engine3D.Scenes;

public static class SceneArgumentsExtensions
{
    public static SceneArguments With<TParameters>(this SceneKey<TParameters> sceneKey, TParameters parameters)
        where TParameters : class =>
        new(sceneKey.Id, provider =>
        {
            if (provider.GetService<SceneParameters<TParameters>>() is not { } sceneParameters)
            {
                return new ResultProblem("Scene '{0}' does not take parameters of type {1}", sceneKey.Id, typeof(TParameters).Name);
            }

            sceneParameters.Set(parameters);
            return Result.Success();
        });
}
