using Olve.Engine3D.Commands;

namespace Olve.Trains.Scenes.GameLogic.Commands;

public static class CommandContextExtensions
{
    public static Result<Id<T>> GetId<T>(this CommandContext commandContext, CommandArgument commandArgument)
        => Id.TryParse<T>(commandContext.Arguments[commandArgument.Key], out var id) ? id : new ResultProblem("Could not parse id '{0}' as Id", commandContext.Arguments[commandArgument.Key]);
}
