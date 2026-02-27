using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameUI.GUI;

namespace Olve.Trains.Scenes.MainMenu;

public class MainMenuStyleService(
    GuiStyleRegistry styleRegistry) : ISceneService
{

    private static readonly IGuiElementStyling[] MainMenuStyles =
    [
        Styles.ModalButtonStyle,
        Styles.ModalButtonTextStyle,
    ];

    public Result Load()
    {
        foreach (var style in MainMenuStyles)
        {
            styleRegistry.Register(style);
        }

        return Result.Success();
    }

    public Result Unload()
    {
        foreach (var style in MainMenuStyles)
        {
            styleRegistry.Unregister(style);
        }

        return Result.Success();
    }
}
