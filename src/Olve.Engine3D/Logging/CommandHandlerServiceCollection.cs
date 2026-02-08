using System.Collections;
using Olve.Engine3D.Commands;
using Olve.Logging;

namespace Olve.Engine3D.Logging;

public class CommandHandlerServiceCollection(ILoggingManager loggingManager) : IEnumerable<ICommandHandler>
{
    private readonly HashSet<CommandHandlerService> _commandHandlerServices = [];
    
    public bool Add(CommandHandlerService commandHandlerService)
    {
        var serviceTypeName = commandHandlerService.GetType().Name;
        loggingManager.Log(LogLevel.Debug, $"[CommandHandlerServiceCollection] Registering {serviceTypeName}", [serviceTypeName, nameof(CommandHandlerServiceCollection)]);
        return _commandHandlerServices.Add(commandHandlerService);
    }

    public bool Remove(CommandHandlerService commandHandlerService)
    {
        var serviceTypeName = commandHandlerService.GetType().Name;
        loggingManager.Log(LogLevel.Debug, $"[CommandHandlerServiceCollection] Deregistering {serviceTypeName}", [serviceTypeName, nameof(CommandHandlerServiceCollection)]);
        return _commandHandlerServices.Remove(commandHandlerService);
    }

    public IEnumerator<ICommandHandler> GetEnumerator() => _commandHandlerServices.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}