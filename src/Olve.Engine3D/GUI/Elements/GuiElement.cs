using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.GUI.Elements;

public abstract class GuiElement : IHasId<Id<GuiElement>>
{
    public required Id<GuiElement> Id { get; set; }
    public required string Name { get; set; }
    public GuiElement[] Children { get; set; } = [];
    public StyleKey? StyleKey { get; set; }
    public bool Interactive { get; init; }
    public bool InheritParentState { get; init; }

    public virtual LayoutBox? LayoutBox => null;
}