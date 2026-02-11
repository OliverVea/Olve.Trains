namespace Olve.Engine3D.Scenes;

public interface ISceneService
{
    int Priority => 0;
    Result Load() => Result.Success();
    Result Unload() => Result.Success();
    Result<Pass> Input(TimeSpan deltaTime) => Result<Pass>.Success(Pass.Pass);
    Result Update(TimeSpan deltaTime) => Result.Success();
    Result Render(TimeSpan deltaTime) => Result.Success();
}
