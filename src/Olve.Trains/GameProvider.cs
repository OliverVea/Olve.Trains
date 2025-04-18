using Jab;
using Olve.Engine3D.Input;
using Olve.Engine3D.Light;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.Console;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Trains;

[ServiceProvider]
[Singleton(typeof(WindowProvider))]
[Singleton(typeof(GLProvider))]
[Singleton(typeof(InputContextProvider))]
[Singleton(typeof(SceneManager))]
[Singleton(typeof(KeyboardManager))]
[Singleton(typeof(MouseManager))]
[Singleton(typeof(MeshEntityManager))]
[Singleton(typeof(ShaderEntityManager))]
[Singleton(typeof(TextureEntityManager))]
[Singleton(typeof(HeightmapEntityManager))]
[Singleton(typeof(RenderingManager))]
[Singleton(typeof(DayTimeManager))]
[Singleton(typeof(DaylightManager))]
[Singleton(typeof(ILoggingManager), typeof(ConsoleLoggingManager))]
public partial class GameProvider
{
    
}

public class NotInitializedException<T>() : Exception($"The {typeof(T).Name} has not been initialized.");

public abstract class Provider<T>
{
    private T? _value;

    public T Value => _value ?? throw new NotInitializedException<T>();

    public void Set(T value)
    {
        _value = value;
    }
}

public class WindowProvider : Provider<IWindow>;
public class GLProvider : Provider<GL>;
public class InputContextProvider : Provider<IInputContext>;

