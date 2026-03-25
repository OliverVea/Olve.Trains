using Olve.Engine3D.GUI.Styling;

namespace Olve.Trains.Scenes.GameUI.GUI;

public static class GameStyleRegistration
{
    private static readonly IGuiElementStyling[] GameStyles =
    [
        Styles.MenuButtonStyle, Styles.MenuBarBackground,
        Styles.ToolIconStyle, Styles.InfoBarBackground,
        Styles.InfoBarSection, Styles.InfoTextMedium,
        Styles.InfoBarButtonStyle,
        Styles.BurgerButtonStyle, Styles.BurgerLineStyle,
        Styles.BurgerOverlayStyle, Styles.BurgerMenuPanelStyle,
        Styles.ModalButtonStyle, Styles.ModalButtonTextStyle,
        Styles.SignalPanelOverlayStyle, Styles.SignalPanelStyle,
        Styles.SignalPanelHeaderStyle, Styles.SignalPanelInfoTextStyle,
        Styles.StationPanelOverlayStyle, Styles.StationPanelStyle,
        Styles.StationPanelHeaderStyle, Styles.StationPanelSubHeaderStyle,
        Styles.StationPanelInfoTextStyle, Styles.StationPanelContainerStyle,
        Styles.InventoryRowStyle, Styles.InventoryRowDirectionStyle,
        Styles.InventoryRowTextStyle, Styles.InventoryRowAmountStyle,
        Styles.SliderThumbStyle,
        Styles.CheckboxBackgroundStyle,
        Styles.DropdownStyle,
        Styles.DropdownOptionStyle
    ];

    public static void RegisterAllStyles(GuiStyleRegistry registry)
    {
        foreach (var style in GameStyles)
        {
            registry.Register(style);
        }
    }
}
