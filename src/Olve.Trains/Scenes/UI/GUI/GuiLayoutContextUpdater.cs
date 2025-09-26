using Olve.Engine3D;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.UI.GUI;

public class GuiLayoutContextUpdater(ILoggingManager loggingManager,
    Provider<IWindow> windowProvider,
    Provider<LayoutContext> layoutContextProvider, 
    GuiElementLayoutService guiElementLayoutService,
    ScreenResizedEvent screenResizedEvent) : SceneService(loggingManager)
{
    private const float UIScale = 1f;
    private const int DesignWidth = 1920;

    private bool _screenResized = true;
    
    protected override Result OnLoad()
    {
        screenResizedEvent.OnWindowResize.Subscribe(OnScreenResized);
        return Result.Success();
    }
    
    protected override Result OnUnload()
    {
        screenResizedEvent.OnWindowResize.Unsubscribe(OnScreenResized);
        return Result.Success();
    }
    
    private void OnScreenResized(Vector2D<int> size) =>  _screenResized = true;

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        if (!_screenResized) return Result.Success();
        _screenResized = false;

        var window = windowProvider.Value;
        var windowSize = window.Size;                 // logical units
        var framebufferSize = window.FramebufferSize; // physical pixels

        float dprX = (float)framebufferSize.X / windowSize.X;
        float dprY = (float)framebufferSize.Y / windowSize.Y;
        float devicePixelRatio = (dprX + dprY) * 0.5f; // usually equal; assert if not

        var designSize = new Vector2D<int>(1920, 1080); // fixed design space

        layoutContextProvider.Set(new LayoutContext(
            windowSize,         // ViewportSize in logical units
            designSize,         // fixed
            devicePixelRatio,
            UIScale
        ));

        return guiElementLayoutService.ComputeLayout();
    }
}