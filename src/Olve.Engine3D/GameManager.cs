using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Events;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Engine3D.TimeStepping;
using Olve.Engine3D.Utilities;
using Olve.Utilities.Ids;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Engine3D;

public class GameManager(
    Provider<IWindow> windowProvider,
    ITimeStepper timeStepper,
    Provider<GL> glProvider,
    Provider<IInputContext> inputContextProvider,
    KeyboardManager keyboardManager,
    MouseManager mouseManager,
    SceneManager sceneManager,
    DeltaTimeService deltaTimeService,
    AfterRenderEvent afterRenderEvent,
    GameClosingEvent gameClosingEvent,
    ILogger<GameManager> logger,
    FaultLogger faultLogger,
    CommandPipeServer? commandPipeServer = null)
{
    private Result _result = Result.Success();
    private Id<IScene> _initialScene;
    private readonly Stopwatch _frameSw = new();
    private const string UpdateSource = $"{nameof(GameManager)}.{nameof(Update)}";
    private const string RenderSource = $"{nameof(GameManager)}.{nameof(Render)}";

    private bool MetricsEnabled => EngineMetrics.IsEnabled;

    public Result Run(Id<IScene> initialScene)
    {
        _initialScene = initialScene;

        timeStepper.Load.Subscribe(OnLoad);
        timeStepper.Render.Subscribe(OnRender);
        timeStepper.Update.Subscribe(OnUpdate);
        timeStepper.Closing.Subscribe(OnClose);

        timeStepper.Run();

        timeStepper.Dispose();

        return _result;
    }


    private void OnLoad()
    {
        if (Load().TryPickProblems(out var problems))
        {
            _result = problems;
            Stop();
        }
    }

    private Result Load()
    {
        if (SetupContexts().TryPickProblems(out var problems, out var contexts))
        {
            return problems.Prepend("Error while setting up contexts");
        }

        var (gl, input) = contexts;
        glProvider.Set(gl);
        inputContextProvider.Set(input);

        if (SetupInput().TryPickProblems(out problems))
        {
            return problems.Prepend("Error while initializing input");
        }

        commandPipeServer?.Start();

        return sceneManager.LoadAndActivateScene(_initialScene);
    }

    private Result<(GL, IInputContext)> SetupContexts() =>
        Result.Concat(
            () => Result.Try<GL, Exception>(() => windowProvider.Value.CreateOpenGL(), "Error while creating OpenGL context"),
            () => Result.Try<IInputContext, Exception>(() => windowProvider.Value.CreateInput(), "Error while creating input context")
        );

    private Result SetupInput() =>
        Result.Chain(
            keyboardManager.Initialize,
            mouseManager.Initialize
        );

    private void OnUpdate(TimeSpan deltaTime)
    {
        if (MetricsEnabled) _frameSw.Restart();

        deltaTimeService.SetFrameDelta(deltaTime);

        ParseFrameResult(Update(), UpdateSource);
    }

    private Result Update()
    {
        Stopwatch? inputSw = MetricsEnabled ? Stopwatch.StartNew() : null;

        var keyboardInputResult = keyboardManager.Input();
        if (keyboardInputResult.TryPickProblems(out var problems))
        {
            return problems.Prepend("Got problem while processing input for KeyboardManager");
        }

        var mouseInputResult = mouseManager.Input();
        if (mouseInputResult.TryPickProblems(out problems))
        {
            return problems.Prepend("Got problem while processing input for MouseManager");
        }

        var sceneInputResult = sceneManager.Input();
        if (sceneInputResult.TryPickProblems(out problems))
        {
            return problems.Prepend("Got problem while processing input for SceneManager");
        }

        if (inputSw is not null)
        {
            inputSw.Stop();
            EngineMetrics.InputDuration.Record(inputSw.Elapsed.TotalMilliseconds);
        }

        Stopwatch? updateSw = MetricsEnabled ? Stopwatch.StartNew() : null;

        var sceneUpdateResult = sceneManager.Update();
        if (sceneUpdateResult.TryPickProblems(out problems))
        {
            return problems.Prepend("Got problem while updating SceneManager");
        }

        if (updateSw is not null)
        {
            updateSw.Stop();
            EngineMetrics.UpdateDuration.Record(updateSw.Elapsed.TotalMilliseconds);
        }

        return Result.Success();
    }

    private void OnRender(TimeSpan deltaTime)
    {
        ParseFrameResult(Render(), RenderSource);

        if (MetricsEnabled)
        {
            _frameSw.Stop();
            EngineMetrics.FrameDuration.Record(_frameSw.Elapsed.TotalMilliseconds);
        }
    }

    private Result Render()
    {
        Stopwatch? renderSw = MetricsEnabled ? Stopwatch.StartNew() : null;

        var result = sceneManager.Render();
        afterRenderEvent.AfterRender.Invoke();

        if (renderSw is not null)
        {
            renderSw.Stop();
            EngineMetrics.RenderDuration.Record(renderSw.Elapsed.TotalMilliseconds);
        }

        return result;
    }

    private void ParseFrameResult(Result result, string source)
    {
        if (!result.TryPickProblems(out var problems))
        {
            faultLogger.LogSuccess(source);
            return;
        }

        if (problems.AnyCritical())
        {
            StopOnCriticalProblem(problems);
            return;
        }

        faultLogger.LogFault(source, problems);
    }

    private void StopOnCriticalProblem(ResultProblemCollection problems)
    {
        logger.LogCritical("Critical problem in the frame loop; stopping the game:{NewLine}{Problems}",
            Environment.NewLine, string.Join(Environment.NewLine, problems.Select(p => p.ToDebugString())));
        _result = problems;
        Stop();
    }

    private void OnClose()
    {
        commandPipeServer?.Dispose();
        sceneManager.Close();
    }

    public void Stop()
    {
        gameClosingEvent.GameClosing.Invoke();
    }
}
