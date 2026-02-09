namespace Olve.Engine3D.GUI;

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