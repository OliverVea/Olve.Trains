namespace Olve.Engine3D.GUI.Styling;

[Flags]
public enum GuiElementState
{
    None = 0,
    Show = 1,
    Enabled = 2,
    Hovered = 4,
    Focused = 8,
    Active = 16
}
