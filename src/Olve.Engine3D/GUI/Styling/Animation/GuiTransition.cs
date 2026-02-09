namespace Olve.Engine3D.GUI.Styling.Animation;

public readonly record struct GuiTransition(Ms Duration, Easing Easing = Easing.Linear)
{
    public static GuiTransition Instant => new(new Ms(0));

    public bool IsInstant => Duration.Value <= 0;
}