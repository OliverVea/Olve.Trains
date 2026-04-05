using Olve.Engine3D.Math;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Environment;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.GameRendering;

public class ClearancePreviewService(
    CollisionSystem collisionSystem,
    EnvironmentalObjectCollisionService environmentalObjectCollisionService,
    EnvironmentalObjectRenderingService environmentalObjectRenderingService)
{
    private static readonly HashSet<Id<ColliderGroup>> AutoClearableGroups = [ColliderGroups.Environment];
    private static readonly Vector3D<float> ClearanceColor = new(1f, 0.3f, 0.3f);
    private const float ClearanceColorMix = 0.7f;

    private readonly HashSet<Id<EnvironmentalObject>> _highlightedObjects = [];

    public void ShowPreview(AABB area)
    {
        var newHighlighted = new HashSet<Id<EnvironmentalObject>>();

        var hits = collisionSystem.QueryOverlapAABB(area, AutoClearableGroups);
        foreach (var hit in hits)
        {
            if (!ColliderGroups.IsAutoClearable(hit.Group)) continue;

            if (environmentalObjectCollisionService.TryGetObjectId(hit.ColliderId, out var objectId))
            {
                newHighlighted.Add(objectId);
            }
        }

        // Clear objects no longer in the preview
        foreach (var objectId in _highlightedObjects)
        {
            if (!newHighlighted.Contains(objectId))
            {
                environmentalObjectRenderingService.ClearColorOverride(objectId);
            }
        }

        // Highlight newly affected objects
        foreach (var objectId in newHighlighted)
        {
            if (!_highlightedObjects.Contains(objectId))
            {
                environmentalObjectRenderingService.SetColorOverride(objectId, ClearanceColor, ClearanceColorMix);
            }
        }

        _highlightedObjects.Clear();
        foreach (var id in newHighlighted) _highlightedObjects.Add(id);
    }

    public void ClearPreview()
    {
        foreach (var objectId in _highlightedObjects)
        {
            environmentalObjectRenderingService.ClearColorOverride(objectId);
        }

        _highlightedObjects.Clear();
    }
}
