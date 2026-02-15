using System.Collections;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;

namespace Olve.Engine3D.Logging;

public class CommandHandlerServiceCollection(ILogger<CommandHandlerServiceCollection> logger) : IEnumerable<ICommandHandler>
{
    private readonly HashSet<ICommandHandler> _commandHandlers = [];

    public bool Add(ICommandHandler commandHandler)
    {
        var serviceTypeName = commandHandler.GetType().Name;
        logger.LogDebug("[CommandHandlerServiceCollection] Registering {ServiceTypeName}", serviceTypeName);
        return _commandHandlers.Add(commandHandler);
    }

    public bool Remove(ICommandHandler commandHandler)
    {
        var serviceTypeName = commandHandler.GetType().Name;
        logger.LogDebug("[CommandHandlerServiceCollection] Deregistering {ServiceTypeName}", serviceTypeName);
        return _commandHandlers.Remove(commandHandler);
    }

    public IEnumerator<ICommandHandler> GetEnumerator() => _commandHandlers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
