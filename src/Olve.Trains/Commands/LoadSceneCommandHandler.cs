using Olve.Engine3D.Commands;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Commands;

public class LoadSceneCommandHandler(SceneManager sceneManager) : ICommandHandler
{
    private const string SceneKey = "scene";

    private static readonly Dictionary<string, Id<IScene>> SceneMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["game"] = SceneIds.GameUIScene,
        ["menu"] = SceneIds.MainMenuScene,
        ["loading"] = SceneIds.LoadingScene,
    };

    public string Verb => "load-scene";
    public string HelpString => "Unloads all scenes and loads the specified scene. Available: game, menu, loading";
    public IReadOnlyList<CommandArgument> Arguments { get; } =
    [
        new(SceneKey, "Scene to load: game, menu, loading", Required: true),
    ];

    public Result<CommandOutput> Handle(CommandContext context)
    {
        var sceneName = context.GetArgument(Arguments[0]);
        if (sceneName is null || !SceneMap.TryGetValue(sceneName, out var sceneId))
        {
            return new ResultProblem(
                "Unknown scene '{0}'. Available: {1}",
                sceneName ?? "",
                string.Join(", ", SceneMap.Keys));
        }

        sceneManager.Close();

        if (sceneManager.LoadAndActivateScene(sceneId).TryPickProblems(out var problems))
        {
            return problems;
        }

        return new CommandOutput($"Loaded scene: {sceneName}");
    }
}
