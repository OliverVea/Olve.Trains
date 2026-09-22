using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Olve.Results.TUnit;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Tests.Scenes;

public class SceneParametersTests
{
    private sealed record ProbeArguments(int Value = 7);

    private sealed record OtherArguments;

    private sealed class ProbeService(ProbeArguments arguments) : ISceneService
    {
        public ProbeArguments Arguments { get; } = arguments;
    }

    private static readonly SceneKey<ProbeArguments> ProbeScene = new(Id.New<IScene>());

    private static (SceneManager SceneManager, ServiceProvider Provider) BuildSceneManager()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<FaultLogger>();
        services.AddSingleton<SceneScopeAccessor>();
        services.AddSingleton(new SceneDefinition(ProbeScene.Id, "ProbeScene"));
        services.AddSingleton<SceneManager>();
        services.AddSceneService<ProbeService>(ProbeScene.Id);
        services.AddSceneParameters(ProbeScene, () => new ProbeArguments());

        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<SceneManager>(), provider);
    }

    private static ProbeArguments LoadedArguments(ServiceProvider provider) =>
        provider.GetRequiredService<SceneScopeAccessor>().ActiveScopeProviders.Single()
            .GetRequiredService<ProbeService>().Arguments;

    [Test]
    public async Task Arguments_Are_Injected_Into_Service_Constructors()
    {
        var (sceneManager, provider) = BuildSceneManager();

        await Assert.That(sceneManager.LoadAndActivateScene(ProbeScene.Id, ProbeScene.With(new ProbeArguments(42))))
            .Succeeded();

        await Assert.That(LoadedArguments(provider).Value).IsEqualTo(42);
    }

    [Test]
    public async Task Loading_Without_Arguments_Injects_Defaults()
    {
        var (sceneManager, provider) = BuildSceneManager();

        await Assert.That(sceneManager.LoadAndActivateScene(ProbeScene.Id)).Succeeded();

        await Assert.That(LoadedArguments(provider).Value).IsEqualTo(7);
    }

    [Test]
    public async Task Arguments_Of_An_Unregistered_Type_Fail_The_Load()
    {
        var (sceneManager, provider) = BuildSceneManager();
        var otherKey = new SceneKey<OtherArguments>(ProbeScene.Id);

        await Assert.That(sceneManager.LoadAndActivateScene(ProbeScene.Id, otherKey.With(new OtherArguments())))
            .Failed();

        await Assert.That(provider.GetRequiredService<SceneScopeAccessor>().ActiveScopeProviders.Any()).IsFalse();
    }

    [Test]
    public async Task Reading_Parameters_Before_The_Scene_Loads_Throws()
    {
        var (_, provider) = BuildSceneManager();
        using var scope = provider.CreateScope();

        await Assert.That(() => scope.ServiceProvider.GetRequiredService<ProbeArguments>())
            .Throws<InvalidOperationException>();
    }
}
