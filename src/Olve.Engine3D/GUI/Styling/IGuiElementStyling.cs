using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Styling.Animation;

namespace Olve.Engine3D.GUI.Styling;

public interface IGuiElementStyling
{
    StyleKey StyleKey { get; }

    IReadOnlyDictionary<GuiNodeState, StateTransition>? StateTransitions { get; }

    bool TryApplyStateWeights(GuiElement guiElement, StateWeights weights);
}