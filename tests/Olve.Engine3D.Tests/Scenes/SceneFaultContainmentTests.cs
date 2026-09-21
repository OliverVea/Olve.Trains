using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Olve.Results.TUnit;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Tests.Scenes;

public class SceneFaultContainmentTests
{
    private sealed class RecordingLogger : ILogger<FaultLogger>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    private class ScriptedService(int priority) : ISceneService
    {
        public int Priority => priority;
        public Func<Result> OnUpdate { get; set; } = Result.Success;
        public Func<Result<Pass>> OnInput { get; set; } = () => Result<Pass>.Success(Pass.Pass);
        public int UpdateCalls { get; private set; }
        public int InputCalls { get; private set; }

        public Result Update()
        {
            UpdateCalls++;
            return OnUpdate();
        }

        public Result<Pass> Input()
        {
            InputCalls++;
            return OnInput();
        }
    }

    private sealed class SiblingService(int priority) : ScriptedService(priority);

    private static Result Fault() => new ResultProblem("injected fault");

    private static Result CriticalFault() =>
        new ResultProblem("injected critical fault") { Severity = ProblemSeverities.Critical };

    private static Scene ActiveScene(ILogger<FaultLogger> logger, params ISceneService[] services) =>
        new(NullLogger<Scene>.Instance, new FaultLogger(logger), services, Id.New<IScene>(), "TestScene")
        {
            State = SceneState.Active
        };

    [Test]
    public async Task NonCritical_Fault_Is_Contained_And_Sibling_Still_Runs()
    {
        var logger = new RecordingLogger();
        var faulty = new ScriptedService(0) { OnUpdate = Fault };
        var sibling = new SiblingService(1);
        var scene = ActiveScene(logger, faulty, sibling);

        for (var frame = 0; frame < 3; frame++)
        {
            await Assert.That(scene.Update()).Succeeded();
        }

        await Assert.That(sibling.UpdateCalls).IsEqualTo(3);
        await Assert.That(logger.Entries.Count(e => e.Level == LogLevel.Error)).IsEqualTo(1);
    }

    [Test]
    public async Task Recovery_Is_Logged_Once_With_Failed_Frame_Count()
    {
        var logger = new RecordingLogger();
        var faulty = new ScriptedService(0) { OnUpdate = Fault };
        var scene = ActiveScene(logger, faulty);

        await Assert.That(scene.Update()).Succeeded();
        await Assert.That(scene.Update()).Succeeded();
        faulty.OnUpdate = Result.Success;
        await Assert.That(scene.Update()).Succeeded();
        await Assert.That(scene.Update()).Succeeded();

        var recoveries = logger.Entries.Where(e => e.Level == LogLevel.Information).ToList();
        await Assert.That(recoveries.Count).IsEqualTo(1);
        await Assert.That(recoveries[0].Message).Contains("2 failed frame");
    }

    [Test]
    public async Task Critical_Fault_Propagates()
    {
        var logger = new RecordingLogger();
        var scene = ActiveScene(logger, new ScriptedService(0) { OnUpdate = CriticalFault });

        await Assert.That(scene.Update()).Failed();
    }

    [Test]
    public async Task Faulting_Input_Service_Passes_And_Later_Block_Still_Applies()
    {
        var logger = new RecordingLogger();
        var faulty = new ScriptedService(0) { OnInput = () => new ResultProblem("injected input fault") };
        var blocker = new ScriptedService(1) { OnInput = () => Result<Pass>.Success(Pass.Block) };
        var afterBlock = new ScriptedService(2);
        var scene = ActiveScene(logger, faulty, blocker, afterBlock);

        var result = scene.Input();

        await Assert.That(result).Succeeded();
        await Assert.That(result.Value).IsEqualTo(Pass.Block);
        await Assert.That(blocker.InputCalls).IsEqualTo(1);
        await Assert.That(afterBlock.InputCalls).IsEqualTo(0);
    }
}
