namespace Olve.Engine3D.Scenes;

public class SceneManager
{
    private readonly List<Scene> _scenes = new();

    public IReadOnlyList<Scene> Scenes => _scenes;

    public Result AddScenes(IEnumerable<Scene> scenes)
    {
        return Result.Chain(scenes.MapResult(AddScene));
    }

    public Result AddScene(Scene scene)
    {
        if (_scenes.Any(x => x.Id == scene.Id))
        {
            return new ResultProblem("Scene with id '{0}' already exists", scene.Id);
        }

        var index = GetInsertIndex(scene.Layer, scene.LayerOrder);

        scene.State = SceneState.Unloaded;
        _scenes.Insert(index, scene);

        return Result.Success();
    }

    public Result RemoveScene(SceneId sceneId)
    {
        if (_scenes.FirstOrDefault(x => x.Id == sceneId) is not {} scene)
        {
            return new ResultProblem("Scene with id '{0}' does not exist", sceneId);
        }

        if (scene.State != SceneState.Unloaded)
        {
            return new ResultProblem("Scene with id '{0}' is not unloaded", sceneId);
        }

        _scenes.Remove(scene);

        return Result.Success();
    }

    public Result LoadScene(SceneId sceneId)
    {
        if (_scenes.FirstOrDefault(x => x.Id == sceneId) is not {} scene)
        {
            return new ResultProblem("Scene with id '{0}' does not exist", sceneId);
        }

        if (scene.State != SceneState.Unloaded)
        {
            return new ResultProblem("Scene with id '{0}' is not unloaded", sceneId);
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

    public Result UnloadScene(SceneId sceneId)
    {
        if (_scenes.FirstOrDefault(x => x.Id == sceneId) is not {} scene)
        {
            return new ResultProblem("Scene with id '{0}' does not exist", sceneId);
        }

        scene.Unload();
        scene.State = SceneState.Unloaded;
        
        return Result.Success();
    }

    public Result ActivateScene(SceneId sceneId)
    {
        if (_scenes.FirstOrDefault(x => x.Id == sceneId) is not {} scene)
        {
            return new ResultProblem("Scene with id '{0}' does not exist", sceneId);
        }

        if (scene.State != SceneState.Inactive)
        {
            return new ResultProblem("Scene with id '{0}' is not inactive", sceneId);
        }

        scene.State = SceneState.Active;

        return Result.Success();
    }

    public Result DeactivateScene(SceneId sceneId)
    {
        if (_scenes.FirstOrDefault(x => x.Id == sceneId) is not {} scene)
        {
            return new ResultProblem("Scene with id '{0}' does not exist", sceneId);
        }

        if (scene.State != SceneState.Active)
        {
            return new ResultProblem("Scene with id '{0}' is not active", sceneId);
        }

        scene.State = SceneState.Inactive;

        return Result.Success();
    }

    public Result Input()
    {
        foreach (var scene in _scenes)
        {
            if (scene.State != SceneState.Active)
            {
                continue;
            }

            var inputResult = scene.Input();
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

    private int GetInsertIndex(SceneLayer layer, int layerOrder)
    {
        var index = 0;

        foreach (var scene in _scenes)
        {
            if (scene.Layer > layer || scene.Layer == layer && scene.LayerOrder > layerOrder)
            {
                index++;
            }
        }

        return index;
    }
}