using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class GuiLayoutContextUpdater(ILogger<GuiLayoutContextUpdater> logger,
    Provider<IWindow> windowProvider,
    Provider<LayoutContext> layoutContextProvider,
    GuiLayoutService guiLayoutService,
    ScreenResizedEvent screenResizedEvent) : ISceneService
{
    private const float UIScale = 1f;

    private bool _screenResized = true;

    public Result Load()
    {
        screenResizedEvent.OnWindowResize.Subscribe(OnScreenResized);
        return Result.Success();
    }

    public Result Unload()
    {
        screenResizedEvent.OnWindowResize.Unsubscribe(OnScreenResized);
        return Result.Success();
    }

    private void OnScreenResized(Vector2D<int> size) => _screenResized = true;

    public Result Update(TimeSpan deltaTime)
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

        logger.LogDebug("Updated layout context: {LayoutContext}", newLayoutContext);

        return Result.Success();
    }
}