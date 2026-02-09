namespace Olve.Engine3D.GUI.Styling.Animation;

public readonly record struct StateTransition(GuiTransition In, GuiTransition Out)
{
    public static StateTransition Instant => new(GuiTransition.Instant, GuiTransition.Instant);
}