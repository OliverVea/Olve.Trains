using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.MainMenu;

public class StartGameCommandHandler(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    IServiceProvider serviceProvider) : CommandHandlerService(commandHandlerServiceCollection)
{
    public override string Verb => "start-game";
    public override string HelpString => "Starts a new game from the main menu";
    public override IReadOnlyList<CommandArgument> Arguments => [];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        if (sceneManager.DeactivateAndUnloadScene(SceneIds.MainMenuScene)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        if (sceneManager.LoadAndActivateScene(SceneIds.GameUIScene)
            .TryPickProblems(out problems))
        {
            return problems;
        }

        return new CommandOutput("Game started");
    }
}
