namespace Olve.Engine3D.Scenes;

public interface ISceneParameterService<in T>
{
    Result LoadParameters(T parameters);
}
