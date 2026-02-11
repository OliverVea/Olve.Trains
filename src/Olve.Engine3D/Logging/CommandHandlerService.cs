using Olve.Engine3D.Commands;
using Olve.Engine3D.Scenes;

namespace Olve.Engine3D.Logging;

public abstract class CommandHandlerService(CommandHandlerServiceCollection commandHandlerServiceCollection) : ISceneService, ICommandHandler
{
    public abstract string Verb { get; }
    public abstract string HelpString { get; }
    public abstract IReadOnlyList<CommandArgument> Arguments { get; }
    public abstract Result Handle(CommandContext commandContext);

    public Result Load()
    {
        commandHandlerServiceCollection.Add(this);
        return Result.Success();
    }

    public Result Unload()
    {
        commandHandlerServiceCollection.Remove(this);
        return Result.Success();
    }
}
