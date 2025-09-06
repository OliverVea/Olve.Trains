using Jab;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Scenes;
using Olve.Logging;

namespace Olve.Trains.Scenes.Console;

[ServiceProvider]
[Transient(typeof(ILoggingManager), Factory= nameof(GetLoggingManager))]
[Transient(typeof(IEnumerable<SceneService>), Factory = nameof(GetAllSceneServices))]
[Transient(typeof(ConsoleSceneProvider), Factory=nameof(GetConsoleSceneProvider))]
[Transient(typeof(CommandHandlerServiceCollection), Factory=nameof(GetCommandHandlerServiceCollection))]
[Singleton(typeof(WebConsoleService))]
public partial class ConsoleSceneProvider(GameProvider gameProvider) : ISceneServicesProvider
{
    private ConsoleSceneProvider GetConsoleSceneProvider() => this;
    
    private ILoggingManager GetLoggingManager() => gameProvider.GetService<ILoggingManager>();

    private CommandHandlerServiceCollection GetCommandHandlerServiceCollection() =>
        gameProvider.GetRequiredService<CommandHandlerServiceCollection>();
    public IEnumerable<SceneService> GetSceneServices() => this.GetServices<SceneService>();

    private static IEnumerable<SceneService> GetAllSceneServices(IServiceProvider provider) =>
    [
        provider.GetRequiredService<WebConsoleService>(),
    ];
}
