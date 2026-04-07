namespace Olve.Engine3D.Scenes;

public interface ISceneParameterService
{
    Result LoadParameters(object parameters);
}

public interface ISceneParameterService<in T> : ISceneParameterService
{
    Result LoadParameters(T parameters);
    Result ISceneParameterService.LoadParameters(object parameters) => LoadParameters((T)parameters);
}
