using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Styling.Animation;

namespace Olve.Engine3D.GUI.Styling;

public class GuiElementStyling<T> : IGuiElementStyling where T : GuiElement
{
    public required StyleKey StyleKey { get; init; }

    public Action<T, StateWeights>? OnStateChanged { get; init; }

    public Dictionary<GuiNodeState, StateTransition>? StateTransitions { get; init; }

    IReadOnlyDictionary<GuiNodeState, StateTransition>? IGuiElementStyling.StateTransitions => StateTransitions;

    public bool TryApplyState(GuiElement guiElement, GuiNodeState state)
    {
        if (guiElement is not T t)
        {
            return false;
        }

        var weights = new StateWeights();
        foreach (GuiNodeState flag in Enum.GetValues<GuiNodeState>())
        {
            if (flag != GuiNodeState.None && flag != GuiNodeState.All && state.HasFlag(flag))
            {
                weights.Set(flag, 1f);
            }
        }

        OnStateChanged?.Invoke(t, weights);
        return true;
    }

    public bool TryApplyStateWeights(GuiElement guiElement, StateWeights weights)
    {
        if (guiElement is not T t)
        {
            return false;
        }

        OnStateChanged?.Invoke(t, weights);
        return true;
    }
}