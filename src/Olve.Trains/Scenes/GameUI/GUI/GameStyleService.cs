using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class GameStyleService(
    GuiStyleApplierService guiStyleApplierService,
    GuiStyleRegistry styleRegistry) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependents([guiStyleApplierService]);

    private static readonly IGuiElementStyling[] GameStyles =
    [
        Styles.MenuButtonStyle, Styles.MenuBarBackground,
        Styles.ToolIconStyle, Styles.InfoBarBackground,
        Styles.InfoBarSection, Styles.InfoTextMedium,
        Styles.InfoBarButtonStyle
    ];

    public Result Load()
    {
        foreach (var style in GameStyles)
        {
            styleRegistry.Register(style);
        }

        return Result.Success();
    }

    public Result Unload()
    {
        foreach (var style in GameStyles)
        {
            styleRegistry.Unregister(style);
        }

        return Result.Success();
    }
}
