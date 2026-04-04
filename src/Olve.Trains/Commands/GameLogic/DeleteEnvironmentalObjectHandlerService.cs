using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Environment;

namespace Olve.Trains.Commands.GameLogic;

public class DeleteEnvironmentalObjectHandlerService(
    ILogger<DeleteEnvironmentalObjectHandlerService> logger,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    EnvironmentalObjectService environmentalObjectService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument ObjectArgument = new("object", "The ID of the environmental object to delete", true);

    public override string Verb => "delete-object";
    public override string HelpString => "Deletes an environmental object by ID. Example: delete-object object=<object-id>";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [ObjectArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<EnvironmentalObject>(ObjectArgument).TryPickProblems(out var problems, out var objectId))
        {
            return problems;
        }

        var result = environmentalObjectService.DeleteObject(objectId);
        if (result.MapToResult(allowNotFound: false).TryPickProblems(out problems))
        {
            return problems;
        }

        logger.LogInformation("Deleted environmental object {ObjectId}", objectId);
        return new CommandOutput($"Deleted environmental object {objectId}");
    }
}
