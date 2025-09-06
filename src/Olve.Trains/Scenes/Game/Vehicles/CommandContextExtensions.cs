using Olve.Engine3D.DebugServer.Commands;

namespace Olve.Trains.Scenes.Game.Vehicles;

public static class CommandContextExtensions
{
    public static Result<Id<T>> GetId<T>(this CommandContext commandContext, CommandArgument commandArgument)
        => Id<T>.Parse(commandContext.Arguments[commandArgument.Key]);
}