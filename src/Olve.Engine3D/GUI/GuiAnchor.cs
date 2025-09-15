using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.GUI;

public readonly record struct GuiAnchor(Id<GuiAnchor> Id) : IHasId<Id<GuiAnchor>>;