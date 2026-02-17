using System.Diagnostics;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Utilities.Ids;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Engine3D;

public class GameManager(Provider<IWindow> windowProvider, Provider<GL> glProvider, Provider<IInputContext> inputContextProvider, KeyboardManager keyboardManager, MouseManager mouseManager, SceneManager sceneManager, ScreenResizedEvent screenResizedEvent, AfterRenderEvent afterRenderEvent, CommandPipeServer? commandPipeServer = null)
{
    private Result _result = Result.Success();
    private Id<IScene> _initialScene;
    private readonly Stopwatch _frameSw = new();

    public Result Run(IWindow window, Id<IScene> initialScene)
    {
        _initialScene = initialScene;
        windowProvider.Set(window);

        window.Load += OnLoad;
        window.Render += OnRender;
        window.Update += OnUpdate;
        window.FramebufferResize += OnFramebufferResize;
        window.Closing += OnClose;

        window.Run();

        window.Dispose();

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

    private void OnUpdate(double deltaSeconds)
    {
        _frameSw.Restart();

        var deltaTime = TimeSpan.FromSeconds(deltaSeconds);
        if (Update(deltaTime).TryPickProblems(out var problems))
        {
            _result = problems;
            Stop();
        }
    }

    private Result Update(TimeSpan deltaTime)
    {
        var inputSw = Stopwatch.StartNew();

        var keyboardInputResult = keyboardManager.Input(deltaTime);
        if (keyboardInputResult.TryPickProblems(out var problems))
        {
            return problems.Prepend("Got problem while processing input for KeyboardManager");
        }

        var mouseInputResult = mouseManager.Input(deltaTime);
        if (mouseInputResult.TryPickProblems(out problems))
        {
            return problems.Prepend("Got problem while processing input for MouseManager");
        }

        var sceneInputResult = sceneManager.Input(deltaTime);
        if (sceneInputResult.TryPickProblems(out problems))
        {
            return problems.Prepend("Got problem while processing input for SceneManager");
        }

        inputSw.Stop();
        EngineMetrics.InputDuration.Record(inputSw.Elapsed.TotalMilliseconds);

        var updateSw = Stopwatch.StartNew();

        var sceneUpdateResult = sceneManager.Update(deltaTime);
        if (sceneUpdateResult.TryPickProblems(out problems))
        {
            return problems.Prepend("Got problem while updating SceneManager");
        }

        updateSw.Stop();
        EngineMetrics.UpdateDuration.Record(updateSw.Elapsed.TotalMilliseconds);

        return Result.Success();
    }

    private void OnRender(double deltaSeconds)
    {
        var deltaTime = TimeSpan.FromSeconds(deltaSeconds);
        if (Render(deltaTime).TryPickProblems(out var problems))
        {
            _result = problems;
            Stop();
        }

        _frameSw.Stop();
        EngineMetrics.FrameDuration.Record(_frameSw.Elapsed.TotalMilliseconds);
    }

    private Result Render(TimeSpan deltaTime)
    {
        var renderSw = Stopwatch.StartNew();

        var result = sceneManager.Render(deltaTime);
        afterRenderEvent.OnAfterRender.Invoke();

        renderSw.Stop();
        EngineMetrics.RenderDuration.Record(renderSw.Elapsed.TotalMilliseconds);

        return result;
    }

    private void OnClose()
    {
        commandPipeServer?.Dispose();
        sceneManager.Close();
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        glProvider.Value.Viewport(size);
        screenResizedEvent.OnWindowResize.Invoke(size);
    }

    public void Stop()
    {
        windowProvider.Value.Close();
    }
}