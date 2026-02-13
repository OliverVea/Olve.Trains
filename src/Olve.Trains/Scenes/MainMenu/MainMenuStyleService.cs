using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.MainMenu;

public class MainMenuStyleService(
    GuiStyleApplierService guiStyleApplierService,
    GuiStyleRegistry styleRegistry) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependents([guiStyleApplierService]);

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
