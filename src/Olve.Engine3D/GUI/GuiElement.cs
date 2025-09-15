using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.GUI;

public readonly record struct GuiElement(Id<GuiElement> Id, string Name) : IHasId<Id<GuiElement>>;