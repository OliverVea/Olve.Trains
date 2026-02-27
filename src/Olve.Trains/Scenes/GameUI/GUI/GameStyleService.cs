using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class GameStyleService(
    GuiStyleRegistry styleRegistry) : ISceneService
{

    private static readonly IGuiElementStyling[] GameStyles =
    [
        Styles.MenuButtonStyle, Styles.MenuBarBackground,
        Styles.ToolIconStyle, Styles.InfoBarBackground,
        Styles.InfoBarSection, Styles.InfoTextMedium,
        Styles.InfoBarButtonStyle,
        Styles.BurgerButtonStyle, Styles.BurgerLineStyle,
        Styles.BurgerOverlayStyle, Styles.BurgerMenuPanelStyle,
        Styles.ModalButtonStyle, Styles.ModalButtonTextStyle
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
