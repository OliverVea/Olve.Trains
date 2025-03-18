using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Engine3D;

public class GameManager(IWindow window, SceneId initialSceneId)
{
    private static GameManager? _instance;
    private static GameManager Instance => _instance ?? throw new ManagersNotInitializedException();

    private readonly IWindow _window = window;
    private readonly SceneId _initialSceneId = initialSceneId;

    private readonly SceneManager _sceneManager = new();
    private readonly KeyboardManager _keyboardManager = new();
    private readonly MouseManager _mouseManager = new();
    private GL? _gl;
    private IInputContext? _input;

    public static IWindow Window => Instance._window;
    public static GL Gl => Instance._gl ?? throw new NotSupportedException("OpenGL has not been initialized");
    public static IInputContext Input => Instance._input ?? throw new NotSupportedException("Input has not been initialized");
    public static SceneManager SceneManager => Instance._sceneManager;
    public static KeyboardManager KeyboardManager => Instance._keyboardManager;
    public static MouseManager MouseManager => Instance._mouseManager;

    private Result _result = Result.Success();

    public static Result Initialize(IWindow window, Scene[] initialScenes, SceneId initialSceneId)
    {
        if (_instance is not null)
        {
            return new ResultProblem("Managers already initialized");
        }

        _instance = new GameManager(window, initialSceneId);

        SceneManager.AddScenes(initialScenes);

        return Result.Success();
    }

    public static Result Run()
    {
        if (_instance is null)
        {
            return new ResultProblem("Managers not initialized");
        }

        Window.Load += Instance.OnLoad;
        Window.Render += Instance.OnRender;
        Window.Update += Instance.OnUpdate;
        Window.FramebufferResize += Instance.OnFramebufferResize;
        Window.Closing += Instance.OnClose;

        Window.Run();

        Window.Dispose();

        return Instance._result;
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

        (_gl, _input) = contexts;

        if (SetupSceneManager().TryPickProblems(out problems))
        {
            return problems.Prepend("Error while setting up scene manager");
        }

        if (SetupInput().TryPickProblems(out problems))
        {
            return problems.Prepend("Error while initializing input");
        }

        return Result.Success();
    }

    private static Result<(GL, IInputContext)> SetupContexts() =>
        Result.Chain(
            () => Result.Try<GL, Exception>(() => Window.CreateOpenGL(), "Error while creating OpenGL context"),
            () => Result.Try<IInputContext, Exception>(() => Window.CreateInput(), "Error while creating input context")
        );

    private static Result SetupSceneManager() =>
        Result.Chain(
            () => SceneManager.LoadScene(Instance._initialSceneId),
            () => SceneManager.ActivateScene(Instance._initialSceneId)
        );

    private static Result SetupInput() =>
        Result.Chain(
            () => KeyboardManager.Initialize(),
            () => MouseManager.Initialize()
        );

    private void OnUpdate(double deltaSeconds)
    {
        var deltaTime = TimeSpan.FromSeconds(deltaSeconds);
        if (Update(deltaTime).TryPickProblems(out var problems))
        {
            _result = problems;
            Stop();
        }
    }

    private Result Update(TimeSpan deltaTime)
    {
        var keyboardInputResult = KeyboardManager.Input(deltaTime);
        if (keyboardInputResult.TryPickProblems(out var problems))
        {
            return problems.Prepend("Got problem while processing input for KeyboardManager");
        }

        var mouseInputResult = MouseManager.Input(deltaTime);
        if (mouseInputResult.TryPickProblems(out problems))
        {
            return problems.Prepend("Got problem while processing input for MouseManager");
        }

        var sceneInputResult = SceneManager.Input();
        if (sceneInputResult.TryPickProblems(out problems))
        {
            return problems.Prepend("Got problem while processing input for SceneManager");
        }

        var sceneUpdateResult = SceneManager.Update(deltaTime);
        if (sceneUpdateResult.TryPickProblems(out problems))
        {
            return problems.Prepend("Got problem while updating SceneManager");
        }

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

    }

    private Result Render(TimeSpan deltaTime)
    {
        return SceneManager.Render(deltaTime);
    }

    private void OnClose()
    {
        SceneManager.Close();
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
    }

    private void Stop()
    {
        Window.Close();
    }
}

public class ManagersNotInitializedException() : Exception("Managers not initialized");

