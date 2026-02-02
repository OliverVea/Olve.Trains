namespace Olve.Engine3D.GUI;

[Flags]
public enum GuiNodeState
{
    None = 0,
    All = ~None,
    Show = 1,
    Enabled = 2,
    Focused = 4,
    Pressed = 8,
    Active = 16
}

public static class GuiNodeStates
{
    public static readonly IReadOnlyCollection<GuiNodeState> Values = [
        GuiNodeState.Show,
        GuiNodeState.Enabled,
        GuiNodeState.Focused,
        GuiNodeState.Pressed,
        GuiNodeState.Active
    ];
}