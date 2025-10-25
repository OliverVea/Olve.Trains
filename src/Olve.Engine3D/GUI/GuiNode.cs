using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.GUI;

public readonly record struct GuiNode(Id<GuiNode> Id, string Name) : IHasId<Id<GuiNode>>;