using System.Collections;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;

namespace Olve.Engine3D.Logging;

public class CommandHandlerServiceCollection(ILogger<CommandHandlerServiceCollection> logger) : IEnumerable<ICommandHandler>
{
    private readonly HashSet<CommandHandlerService> _commandHandlerServices = [];

    public bool Add(CommandHandlerService commandHandlerService)
    {
        var serviceTypeName = commandHandlerService.GetType().Name;
        logger.LogDebug("[CommandHandlerServiceCollection] Registering {ServiceTypeName}", serviceTypeName);
        return _commandHandlerServices.Add(commandHandlerService);
    }

    public bool Remove(CommandHandlerService commandHandlerService)
    {
        var serviceTypeName = commandHandlerService.GetType().Name;
        logger.LogDebug("[CommandHandlerServiceCollection] Deregistering {ServiceTypeName}", serviceTypeName);
        return _commandHandlerServices.Remove(commandHandlerService);
    }

    public IEnumerator<ICommandHandler> GetEnumerator() => _commandHandlerServices.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
