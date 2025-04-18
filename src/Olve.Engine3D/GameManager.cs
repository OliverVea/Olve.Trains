using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Input;
using Olve.Engine3D.Light;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Scenes;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Engine3D;



public class GameManager(IWindow window, SceneId[] initialSceneIds)
{
    private static GameManager? _instance;
    private static GameManager Instance => _instance ?? throw new ManagersNotInitializedException();

    private readonly IWindow _window = window;
    private readonly SceneId[] _initialSceneIds = initialSceneIds;

    private readonly SceneManager _sceneManager = new();

    // IO
    private readonly KeyboardManager _keyboardManager = new();
    public static KeyboardManager KeyboardManager => Instance._keyboardManager;
    private readonly MouseManager _mouseManager = new();
    public static MouseManager MouseManager => Instance._mouseManager;

    // Rendering
    private readonly MeshEntityManager _meshEntityManager = new();
    public static MeshEntityManager MeshEntityManager => Instance._meshEntityManager;

    private readonly ShaderEntityManager _shaderManager = new();
    public static ShaderEntityManager ShaderEntityManager => Instance._shaderManager;

    private readonly TextureEntityManager _textureManager = new();
    public static TextureEntityManager TextureEntityManager => Instance._textureManager;
    private readonly HeightmapEntityManager _heightmapEntityManager = new();
    public static HeightmapEntityManager HeightmapEntityManager => Instance._heightmapEntityManager;

    private readonly RenderingManager _renderingManager = new();
    public static RenderingManager RenderingManager => Instance._renderingManager;

    // Game
    private readonly DayTimeManager _dayTimeManager = new();
    public static DayTimeManager DayTimeManager => Instance._dayTimeManager;

    private readonly DaylightManager _daylightManager = new();
    public static DaylightManager DaylightManager => Instance._daylightManager;

    public static ILoggingManager LoggingManager { get; set; } = new NoopLoggingManager();

    // Contexts
    private GL? _gl;
    private IInputContext? _input;

    public static IWindow Window => Instance._window;
    public static GL GL => Instance._gl ?? throw new NotSupportedException("OpenGL has not been initialized");
    public static IInputContext Input => Instance._input ?? throw new NotSupportedException("Input has not been initialized");
    public static SceneManager SceneManager => Instance._sceneManager;

    // ServiceProvider
    private IServiceCollection? _serviceProvider;
    public static IServiceCollection Services => Instance._serviceProvider ?? throw new NotSupportedException("ServiceProvider has not been initialized");


    private Result _result = Result.Success();

    public Result Initialize(IWindow window, Scene[] initialScenes, SceneId[] initialSceneIds)
    {
        if (_instance is not null)
        {
            return new ResultProblem("Managers already initialized");
        }

        _instance = new GameManager(window, initialSceneIds);

        SceneManager.AddScenes(initialScenes);

        return Result.Success();
    }

    public Result Run()
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

        SetupDependencyInjection();

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
        Result.Concat(
            () => Result.Try<GL, Exception>(() => Window.CreateOpenGL(), "Error while creating OpenGL context"),
            () => Result.Try<IInputContext, Exception>(() => Window.CreateInput(), "Error while creating input context")
        );

    private static Result SetupSceneManager()
    {
        var results = Instance._initialSceneIds.Select(id =>
            Result.Chain(
                () => SceneManager.LoadScene(id),
                () => SceneManager.ActivateScene(id)));

        if (results.TryPickProblems(out var problems))
        {
            return problems.Prepend("Error while setting up initial scenes");
        }

        return Result.Success();
    }

    private static Result SetupInput() =>
        Result.Chain(
            () => KeyboardManager.Initialize(),
            () => MouseManager.Initialize()
        );

    private static void SetupDependencyInjection()
    {
        _instance._serviceProvider = new ServiceCollection();

        _instance._serviceProvider.AddSingleton(_instance);
        _instance._serviceProvider.AddSingleton(_instance._window);
        _instance._serviceProvider.AddSingleton(_instance._gl);
        _instance._serviceProvider.AddSingleton(_instance._input);
        _instance._serviceProvider.AddSingleton(_instance._meshEntityManager);
        _instance._serviceProvider.AddSingleton(_instance._shaderManager);
        _instance._serviceProvider.AddSingleton(_instance._textureManager);
        _instance._serviceProvider.AddSingleton(_instance._heightmapEntityManager);
        _instance._serviceProvider.AddSingleton(_instance._renderingManager);
        _instance._serviceProvider.AddSingleton(_instance._keyboardManager);
        _instance._serviceProvider.AddSingleton(_instance._mouseManager);
        _instance._serviceProvider.AddSingleton(_instance._dayTimeManager);
        _instance._serviceProvider.AddSingleton(_instance._daylightManager);
        _instance._serviceProvider.AddSingleton(_instance._sceneManager);
    }

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

    public void Stop()
    {
        Window.Close();
    }

    public static void StopGame() => Instance.Stop();
}

public class ManagersNotInitializedException() : Exception("Managers not initialized");

