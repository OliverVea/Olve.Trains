using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Input;

public readonly record struct GuiFocusChanged(
    Id<GuiNode>? Previous,
    Id<GuiNode>? Current);