using Olve.Engine3D.GUI.Layout;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.GUI.Elements;

public abstract class GuiElement : IHasId<Id<GuiElement>>
{
    public required Id<GuiElement> Id { get; set; }
    public required string Name { get; set; }
    public GuiElement[] Children { get; set; } = [];


    public virtual LayoutBox? LayoutBox => null;
}