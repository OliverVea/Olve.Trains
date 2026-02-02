namespace Olve.Engine3D.GUI.Styling.Animation;

public class StateWeights
{
    private readonly Dictionary<GuiNodeState, float> _weights = new();

    public float this[GuiNodeState state] => _weights.GetValueOrDefault(state, 0f);

    public void Set(GuiNodeState state, float weight) => _weights[state] = weight;

    public void Clear() => _weights.Clear();
}
