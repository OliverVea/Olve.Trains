namespace Olve.Engine3D.Scenes;

public interface ISceneServicesProvider
{
    IEnumerable<SceneService> GetSceneServices();
}