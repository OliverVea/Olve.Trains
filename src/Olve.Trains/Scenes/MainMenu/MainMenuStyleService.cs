using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.MainMenu;

public class MainMenuStyleService(
    GuiStyleRegistry styleRegistry) : ISceneService
{

    private static readonly IGuiElementStyling[] Styles =
    [
        MainMenuStyles.MainMenuButtonStyle,
        MainMenuStyles.MainMenuButtonTextStyle,
    ];

    public Result Load()
    {
        foreach (var style in Styles)
        {
            styleRegistry.Register(style);
        }

        return Result.Success();
    }

    public Result Unload()
    {
        foreach (var style in Styles)
        {
            styleRegistry.Unregister(style);
        }

        return Result.Success();
    }
}
