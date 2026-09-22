using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Scenes;
using Olve.Trains.Saves;
using Olve.Trains.Scenes.Loading;

namespace Olve.Trains.Commands.GameLogic;

public class LoadGameHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    SceneManager sceneManager,
    SaveFileStore saveFileStore) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument KindArgument = new("kind", "Save kind: manual, quicksave, or autosave", true);
    private static readonly CommandArgument NameArgument = new("name", "Save name (required for manual saves)");

    public override string Verb => "load-game";
    public override string HelpString => "Loads a save file: tears down the current game and reloads through the loading scene";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [KindArgument, NameArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetRequiredArgument(KindArgument).Bind(ParseKind)
            .TryPickProblems(out var problems, out var kind))
        {
            return problems;
        }

        if (ResolveName(kind, commandContext.GetOptionalArgument(NameArgument))
            .TryPickProblems(out problems, out var name))
        {
            return problems;
        }

        if (saveFileStore.ResolvePath(kind, name).TryPickProblems(out problems, out var path))
        {
            return problems;
        }

        // Fail before tearing down the running game so a bad request leaves the session intact.
        if (!path.Exists())
        {
            return new ResultProblem("Save file '{0}' does not exist.", path.Path);
        }

        // The loading scene re-reads and deserializes the save on a background thread, then transitions to game.
        sceneManager.Close();
        if (sceneManager.LoadAndActivateScene(
                SceneIds.LoadingScene.Id, SceneIds.LoadingScene.With(new LoadingSceneArguments(path.Path)))
            .TryPickProblems(out problems))
        {
            return problems;
        }

        var json = JsonSerializer.Serialize(new { path = path.Path });
        return new CommandOutput(json);
    }

    private static Result<SaveKind> ParseKind(string input) => input.ToLowerInvariant() switch
    {
        "manual" => SaveKind.Manual,
        "quicksave" or "quick" => SaveKind.Quicksave,
        "autosave" or "auto" => SaveKind.Autosave,
        _ => new ResultProblem("Invalid save kind '{0}'. Use: manual, quicksave, or autosave.", input),
    };

    private static Result<string> ResolveName(SaveKind kind, string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return kind switch
        {
            SaveKind.Manual => new ResultProblem("Manual saves require a 'name' argument."),
            SaveKind.Quicksave => "quicksave",
            SaveKind.Autosave => "autosave",
            _ => new ResultProblem("Unknown save kind '{0}'.", kind),
        };
    }
}
