using Olve.Engine3D.DebugServer.Commands;
using Olve.Engine3D.Scenes;
using Olve.Logging;

namespace Olve.Engine3D.Logging;

public abstract class CommandHandlerService(ILoggingManager loggingManager, CommandHandlerServiceCollection commandHandlerServiceCollection) : SceneService(loggingManager), ICommandHandler
{
    public abstract string Verb { get; }
    public abstract string HelpString { get; }
    public abstract IReadOnlyList<CommandArgument> Arguments { get; }
    public abstract Result Handle(CommandContext commandContext);

    protected override Result OnLoad()
    {
        commandHandlerServiceCollection.Add(this);
        return Result.Success();
    }

    protected override Result OnUnload()
    {
        commandHandlerServiceCollection.Remove(this);
        return Result.Success();
    }
}