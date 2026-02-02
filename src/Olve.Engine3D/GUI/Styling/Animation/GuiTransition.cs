namespace Olve.Engine3D.GUI.Styling.Animation;

public readonly record struct GuiTransition(Ms Duration, Easing Easing = Easing.Linear)
{
    public static GuiTransition Instant => new(new Ms(0));

    public bool IsInstant => Duration.Value <= 0;
}

public readonly record struct StateTransition(GuiTransition In, GuiTransition Out)
{
    public static StateTransition Instant => new(GuiTransition.Instant, GuiTransition.Instant);
}
