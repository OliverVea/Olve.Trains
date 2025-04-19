using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public class SceneManager(IEnumerable<IScene> scenes)
{
    private readonly List<IScene> _scenes = scenes.OrderBy(x => x.LayerOrder).ThenBy(x => x.Layer).ToList();

    public Result LoadScene(Id<IScene> sceneId)
    {
        if (_scenes.FirstOrDefault(x => x.Id == sceneId) is not {} scene)
        {
            return new ResultProblem("IScene with id '{0}' does not exist", sceneId);
        }

        if (scene.State != SceneState.Unloaded)
        {
            return new ResultProblem("IScene with id '{0}' is not unloaded", sceneId);
        }

        var loadResult = scene.Load();
        if (loadResult.TryPickProblems(out var problems))
        {
            scene.State = SceneState.Unloaded;
            return problems.Prepend("Error occurred while loading scene with id '{0}'", sceneId);
        }

        scene.State = SceneState.Inactive;
        
        return Result.Success();
    }

    public Result UnloadScene(Id<IScene> sceneId)
    {
        if (_scenes.FirstOrDefault(x => x.Id == sceneId) is not {} scene)
        {
            return new ResultProblem("IScene with id '{0}' does not exist", sceneId);
        }

        scene.Unload();
        scene.State = SceneState.Unloaded;
        
        return Result.Success();
    }

    public Result ActivateScene(Id<IScene> sceneId)
    {
        if (_scenes.FirstOrDefault(x => x.Id == sceneId) is not {} scene)
        {
            return new ResultProblem("IScene with id '{0}' does not exist", sceneId);
        }

        if (scene.State != SceneState.Inactive)
        {
            return new ResultProblem("IScene with id '{0}' is not inactive", sceneId);
        }

        scene.State = SceneState.Active;

        return Result.Success();
    }

    public Result DeactivateScene(Id<IScene> sceneId)
    {
        if (_scenes.FirstOrDefault(x => x.Id == sceneId) is not {} scene)
        {
            return new ResultProblem("IScene with id '{0}' does not exist", sceneId);
        }

        if (scene.State != SceneState.Active)
        {
            return new ResultProblem("IScene with id '{0}' is not active", sceneId);
        }

        scene.State = SceneState.Inactive;

        return Result.Success();
    }

    public Result Input(TimeSpan gameTime)
    {
        foreach (var scene in _scenes)
        {
            if (scene.State != SceneState.Active)
            {
                continue;
            }

            var inputResult = scene.Input(gameTime);
            if (inputResult.TryPickProblems(out var problems, out var passInput))
            {
                return problems.Prepend("Got problem while updating scene input for scene '{0}'", scene.Id);
            }

            if (passInput == Pass.Block)
            {
                break;
            }
        }

        return Result.Success();
    }

    public Result Update(TimeSpan gameTime)
    {
        foreach (var scene in _scenes)
        {
            if (scene.State != SceneState.Active)
            {
                continue;
            }

            var updateResult = scene.Update(gameTime);
            if (updateResult.TryPickProblems(out var problems))
            {
                return problems.Prepend("Got problem while updating scene for scene '{0}'", scene.Id);
            }
        }

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        foreach (var scene in _scenes)
        {
            if (scene.State != SceneState.Active)
            {
                continue;
            }

            var renderResult = scene.Render(deltaTime);
            if (renderResult.TryPickProblems(out var problems))
            {
                return problems.Prepend("Got problem while rendering scene for scene '{0}'", scene.Id);
            }
        }

        return Result.Success();
    }

    public void Close()
    {
        foreach (var scene in _scenes)
        {
            if (scene.State == SceneState.Unloaded)
            {
                continue;
            }

            scene.Unload();
        }
    }
}