using Olve.Engine3D.GUI.Layout;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.GUI;

public readonly record struct GuiAnchor(
    Id<GuiAnchor> Id,
    AnchorPosition Position,
    GrowthDirection Growth,
    int Depth = 0) : IHasId<Id<GuiAnchor>>;