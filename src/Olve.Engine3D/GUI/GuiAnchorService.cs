using Olve.Engine3D.GUI.Layout;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI;

public class GuiAnchorService
{
    private readonly Dictionary<Id<GuiAnchor>, GuiAnchor> _anchors = new();

    public Result<Id<GuiAnchor>> RegisterAnchor(AnchorPosition position, GrowthDirection growth)
    {
        var anchorId = Id.New<GuiAnchor>();
        var anchor = new GuiAnchor(anchorId, position, growth);
        _anchors[anchorId] = anchor;
        return anchorId;
    }

    public bool TryGetAnchor(Id<GuiAnchor> anchorId, out GuiAnchor anchor)
        => _anchors.TryGetValue(anchorId, out anchor);

    public void UnregisterAnchor(Id<GuiAnchor> anchorId)
        => _anchors.Remove(anchorId);
}
