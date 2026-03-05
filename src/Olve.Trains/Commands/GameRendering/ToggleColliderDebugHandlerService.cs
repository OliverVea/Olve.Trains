using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameRendering;

namespace Olve.Trains.Commands.GameRendering;

public class ToggleColliderDebugHandlerService(
    ColliderDebugSettings settings,
    CommandHandlerServiceCollection commandHandlerServiceCollection)
    : CommandHandlerService(commandHandlerServiceCollection)
{
    public override string Verb => "toggle-collider-debug";
    public override string HelpString => "Toggles the collider debug wireframe renderer on/off";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        settings.IsEnabled = !settings.IsEnabled;
        return new CommandOutput($"Collider debug: {(settings.IsEnabled ? "on" : "off")}");
    }
}
