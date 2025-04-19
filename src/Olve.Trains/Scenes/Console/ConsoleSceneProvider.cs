using Jab;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Input;
using Olve.Engine3D.Light;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.Console;

[ServiceProvider(RootServices =  [typeof(SceneService)])]
[Singleton(typeof(ConsoleService))]
[Singleton(typeof(SceneService), Factory = nameof(GetConsoleService))]
[Singleton(typeof(ConsoleCommandService))]
[Singleton(typeof(ILoggingManager), Factory = nameof(GetLoggingManager))]
[Singleton(typeof(DayTimeManager), Factory = nameof(GetDayTimeManager))]
[Singleton(typeof(KeyboardManager), Factory = nameof(GetKeyboardManager))]
[Singleton(typeof(IEnumerable<SceneService>), Factory = nameof(GetAllSceneServices))]
public partial class ConsoleSceneProvider(GameProvider gameProvider) : ISceneServicesProvider
{
    private static ConsoleService GetConsoleService(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<ConsoleService>();
    
    
    private ILoggingManager GetLoggingManager() => gameProvider.GetService<ILoggingManager>();
    private DayTimeManager GetDayTimeManager() => gameProvider.GetService<DayTimeManager>();
    private KeyboardManager GetKeyboardManager() => gameProvider.GetService<KeyboardManager>();
    public IEnumerable<SceneService> GetSceneServices() => this.GetServices<SceneService>();
    
    private IEnumerable<SceneService> GetAllSceneServices(IServiceProvider provider) =>
    [
        provider.GetRequiredService<ConsoleService>(),
    ];
}
