using Facebook.Yoga;
using Olve.Engine3D;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.UI.GUI;

public abstract class BaseGuiContainer(YogaConfig yogaConfig) : BaseGuiElement(yogaConfig)
{
    private readonly List<BaseGuiElement> _children = [];
    public IReadOnlyList<BaseGuiElement> Children => _children.AsReadOnly();

    public void AddChild(BaseGuiElement child)
    {
        _children.Add(child);
        RootNode.AddChild(child.RootNode);
    }

    public void RemoveChild(BaseGuiElement child)
    {
        _children.Remove(child);
        RootNode.RemoveChild(child.RootNode);
    }

    public override void Draw(GL gl)
    {
        if (!Enabled) return;

        OnRender(gl);

        foreach (var child in _children)
        {
            child.Draw(gl);
        }
    }
}

public abstract class BaseGuiElement(YogaConfig yogaConfig)
{
    public YogaNode RootNode { get; } = new(yogaConfig);

    public bool Enabled { get; set; } = true;

    public abstract void OnRender(GL gl);
    
    public virtual void Draw(GL gl)
    {
        if (!Enabled) return;

        OnRender(gl);
    }
}

public sealed class GuiContainer(YogaConfig yogaConfig) : BaseGuiContainer(yogaConfig)
{
    public override void OnRender(GL gl)
    {
        gl.
    }
}

public class LayoutRoot
{
    
}

public class ResourceBarService(ILoggingManager loggingManager, ScreenResizedEvent screenResizedEvent, Provider<IWindow> windowProvider) : SceneService(loggingManager)
{
    private readonly YogaNode _rootNode = new();

    protected override Result OnLoad()
    {
        screenResizedEvent.OnWindowResize.Subscribe(OnWindowResize);

        GL gl;
        gl.sciss
        
        _rootNode.Width = windowProvider.Value.FramebufferSize.X;
        _rootNode.Height = YogaValue.Point(50);
        
        _rootNode.Margin = YogaValue.Point(4);

        var config = new YogaConfig()
        {
            PointScaleFactor = 
        }
        return Result.Success();
    }

    protected override Result OnUnload()
    {
        _rootNode.Clear();
        
        screenResizedEvent.OnWindowResize.Unsubscribe(OnWindowResize);
        
        return Result.Success();
    }

    private void OnWindowResize(Vector2D<int> newSize)
    {
        _rootNode.Width = newSize.X;
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        _rootNode.CalculateLayout();
        return Result.Success();
    }

    private Vector2D<int> _lastSize = Vector2D<int>.Zero;
    
    protected override Result OnRender(TimeSpan deltaTime)
    {
        var size = new Vector2D<int>((int)_rootNode.LayoutWidth, (int)_rootNode.LayoutHeight);
        if (_lastSize == size)
        {
            return Result.Success();
        }
        
        _lastSize = size;
        
        LoggingManager.Log(LogLevel.Debug, $"Size is now {size}!");
        return Result.Success();
    }
}