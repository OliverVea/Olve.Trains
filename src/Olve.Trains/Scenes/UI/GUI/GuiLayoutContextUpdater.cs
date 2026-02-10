using Olve.Engine3D;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Logging;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.UI.GUI;

public class GuiLayoutContextUpdater(ILoggingManager loggingManager,
    Provider<IWindow> windowProvider,
    Provider<LayoutContext> layoutContextProvider,
    GuiLayoutService guiLayoutService,
    ScreenResizedEvent screenResizedEvent) : SceneService(loggingManager)
{
    private const float UIScale = 1f;

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

    private void OnScreenResized(Vector2D<int> size) => _screenResized = true;

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        if (!_screenResized) return Result.Success();
        _screenResized = false;

        var window = windowProvider.Value;
        var aspectRatio = (float) window.Size.X / window.Size.Y;

        var designWidth = MathF.Round(window.Size.X * UIScale);
        var designHeight = designWidth /  aspectRatio;

        var designSize = new Vector2D<Dp>(designWidth, designHeight);
        DpPxRatio designPixelRatio = new(designWidth / window.Size.X);

        LayoutContext newLayoutContext = new(
            designSize,
            aspectRatio,
            designPixelRatio,
            UIScale
        );

        layoutContextProvider.Set(newLayoutContext);
        guiLayoutService.SetDirty();

        LoggingManager.Log(LogLevel.Debug, $"Updated layout context: {newLayoutContext}");

        return Result.Success();
    }
}