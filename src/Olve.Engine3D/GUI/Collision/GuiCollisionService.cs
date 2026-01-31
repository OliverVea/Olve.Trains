using Olve.Engine3D.GUI.Layout;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Collision;

public class GuiCollisionService(GuiLayoutService guiLayoutService)
{
    public IEnumerable<Id<GuiNode>> GetGuiCollisions(Vector2D<Px> position)
    {
        return guiLayoutService
            .GetNodePositions()
            .Where(x => Contains(x.Position, position))
            .Select(x => x.NodeId);
    }

    private static bool Contains(BoxPosition boxPosition, Vector2D<Px> position)
    {
        var boxX = boxPosition.Position.X + boxPosition.Size.X;
        var boxY = boxPosition.Position.Y + boxPosition.Size.Y;

        return position.X >= boxPosition.Position.X
               && position.X <= boxX
               && position.Y >= boxPosition.Position.Y
               && position.Y <= boxY;

    }
}