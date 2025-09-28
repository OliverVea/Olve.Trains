namespace Olve.Trains.resources.layouts;

// Generated START
public static class InfoBar
{
    public readonly record struct ElementsWithId(Box InfoBar);

    public static (Box, ElementsWithId) Build()
    {
        var infoBar = new Box
        {
            Width = null,
            Height = 50,
            Weight = 1,
            Children =
            [
            ]
        };

        ElementsWithId elementsWithId = new(infoBar);

        return (infoBar, elementsWithId);
    }
}
// Generated END



// Will be defined properly later.
public class GuiElement
{
    
}

public class Box
{
    public int? Width { get; set; } = null;
    public int? Height { get; set; } = null;
    public int Weight { get; set; } = 0;
    public IReadOnlyList<GuiElement> Children { get; set; } = new List<GuiElement>();
}